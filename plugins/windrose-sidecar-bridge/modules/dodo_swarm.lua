local creature_specs = {
  ["R5.Creature.Dodo"] = { name = "Dodo", assetPath = "/Game/Gameplay/Character/AI/Mob/Dodo/BP_Mob_Dodo", assetName = "BP_Mob_Dodo" },
  ["R5.Creature.Wolf"] = { name = "Wolf", assetPath = "/Game/Gameplay/Character/AI/Mob/Wolf/BP_Mob_Wolf", assetName = "BP_Mob_Wolf" }
}

local function read_file(path)
  local file = io.open(path, "r")
  if not file then
    return nil
  end

  local body = file:read("*a")
  file:close()
  return body
end

local function valid_uobject(obj)
  if not obj then return false end
  local ok, is_valid = pcall(function() return obj:IsValid() end)
  return ok and is_valid == true
end

local function player_name(pc)
  local name = nil
  pcall(function()
    local ps = pc.PlayerState
    if valid_uobject(ps) then
      local value = ps.PlayerNamePrivate or ps.PlayerName
      if value then
        local ok, str = pcall(function() return value:ToString() end)
        name = ok and str or tostring(value)
      end
    end
  end)
  return name
end

local function target_matches(target, p_name, pawn_name, pawn_full_name)
  if not target or target == "" then return false end
  local needle = tostring(target):lower()
  for _, candidate in ipairs({ p_name, pawn_name, pawn_full_name }) do
    if candidate and tostring(candidate):lower():find(needle, 1, true) then return true end
  end
  return false
end

local function resolve_target_pawn(target, plugin_id)
  local admin = WindrosePlus and WindrosePlus._modules and WindrosePlus._modules.Admin or nil
  if admin and type(admin._findPlayersByName) == "function" then
    local ok, players = pcall(admin._findPlayersByName, target)
    if ok and type(players) == "table" and #players == 1 and valid_uobject(players[1].pawn) then
      local p = players[1]
      return p.pawn, p.displayName or p.name or p.actorName or target
    end
    if ok and type(players) == "table" and #players > 1 then
      return nil, "Target player ambiguous in WindrosePlus Admin cache: " .. tostring(target)
    end
    ok, players = pcall(admin._getPlayers)
    if ok and type(players) == "table" and #players == 1 and valid_uobject(players[1].pawn) then
      local p = players[1]
      print("[" .. tostring(plugin_id or "windrose-sidecar-bridge") .. "] target lookup used WindrosePlus Admin single-online-pawn fallback requestedTarget=" .. tostring(target))
      return p.pawn, p.displayName or p.name or p.actorName or target
    end
  end

  local pcs = FindAllOf and FindAllOf("PlayerController") or nil
  if not pcs then return nil, "No PlayerController instances found" end
  local fallback_pawn = nil
  local fallback_label = nil
  local fallback_count = 0
  for _, pc in ipairs(pcs) do
    if valid_uobject(pc) then
      local p_name = player_name(pc)
      local pc_full_name = nil
      pcall(function() pc_full_name = pc:GetFullName() end)
      local pawn = nil
      pcall(function() pawn = pc.Pawn end)
      if valid_uobject(pawn) then
        local pawn_full_name = nil
        local pawn_name = nil
        pcall(function()
          pawn_full_name = pawn:GetFullName()
          pawn_name = pawn_full_name and (pawn_full_name:match("([^%.]+)$") or pawn_full_name) or nil
        end)
        fallback_pawn = pawn
        fallback_label = p_name or pawn_name or pc_full_name or target
        fallback_count = fallback_count + 1
        if target_matches(target, p_name, pawn_name, pawn_full_name) or target_matches(target, pc_full_name, nil, nil) then
          return pawn, fallback_label
        end
      end
    end
  end
  if fallback_count == 1 then
    print("[" .. tostring(plugin_id or "windrose-sidecar-bridge") .. "] target lookup used single-online-pawn fallback requestedTarget=" .. tostring(target) .. " resolved=" .. tostring(fallback_label))
    return fallback_pawn, fallback_label or target
  end
  return nil, "Target player not found or ambiguous valid pawn count=" .. tostring(fallback_count) .. ": " .. tostring(target)
end

local asset_registry_helpers = nil
local function load_actor_class(spec)
  if not spec then return nil, "Unsupported creature" end
  if not asset_registry_helpers then
    if type(StaticFindObject) ~= "function" then return nil, "StaticFindObject unavailable" end
    asset_registry_helpers = StaticFindObject("/Script/AssetRegistry.Default__AssetRegistryHelpers")
  end
  if not valid_uobject(asset_registry_helpers) then return nil, "AssetRegistryHelpers unavailable" end
  if not UEHelpers or type(UEHelpers.FindOrAddFName) ~= "function" then return nil, "UEHelpers.FindOrAddFName unavailable" end

  local asset_data = {
    PackageName = UEHelpers.FindOrAddFName(spec.assetPath),
    AssetName = UEHelpers.FindOrAddFName(spec.assetName)
  }
  local actor_class = asset_registry_helpers:GetAsset(asset_data)
  if valid_uobject(actor_class) then return actor_class, nil end

  -- UE4 fallback shape used by BPModLoader on older builds.
  asset_data = { ObjectPath = UEHelpers.FindOrAddFName(spec.assetPath .. "." .. spec.assetName) }
  actor_class = asset_registry_helpers:GetAsset(asset_data)
  if valid_uobject(actor_class) then return actor_class, nil end

  return nil, "Failed to load actor class " .. spec.assetPath .. "." .. spec.assetName
end

local function resolve_world_from_pawn(pawn)
  local world = nil
  pcall(function() if pawn.GetWorld then world = pawn:GetWorld() end end)
  if valid_uobject(world) then return world end
  local worlds = FindAllOf and FindAllOf("World") or nil
  if worlds then
    for _, candidate in ipairs(worlds) do
      if valid_uobject(candidate) then
        local full_name = ""
        pcall(function() full_name = candidate:GetFullName() end)
        if full_name:find("GenlandiaMulty", 1, true) then return candidate end
        if not world then world = candidate end
      end
    end
  end
  return world
end

local function spawn_creature_near_pawn(action, pawn, target_label, plugin_id)
  local spec = creature_specs[action.creatureId] or creature_specs["R5.Creature.Dodo"]
  local count = tonumber(action.count) or 1
  if count < 1 then count = 1 end
  if count > 12 then count = 12 end -- dev smoke guard: keep accidental swarms small.

  local actor_class, class_error = load_actor_class(spec)
  if not actor_class then return false, class_error, 0 end

  local world = resolve_world_from_pawn(pawn)
  if not valid_uobject(world) then return false, "Unable to resolve valid world", 0 end

  local base = { X = 0, Y = 0, Z = 0 }
  pcall(function()
    local loc = pawn:K2_GetActorLocation()
    if loc then base = { X = loc.X or 0, Y = loc.Y or 0, Z = loc.Z or 0 } end
  end)

  local radius = tonumber(action.radiusMeters) or 6
  if radius < 2 then radius = 2 end
  if radius > 20 then radius = 20 end
  local spawned = 0
  local last_error = nil

  for i = 1, count do
    local angle = ((i - 1) / count) * 6.28318530718
    local dist = radius * 100.0
    local loc = { X = base.X + math.cos(angle) * dist, Y = base.Y + math.sin(angle) * dist, Z = base.Z + 80.0 }
    local ok, actor = pcall(function() return world:SpawnActor(actor_class, {}, {}) end)
    if ok and valid_uobject(actor) then
      pcall(function() actor:K2_SetActorLocation(loc, false, {}, true) end)
      spawned = spawned + 1
      local actor_name = "unknown"
      pcall(function() actor_name = actor:GetFullName() end)
      print("[" .. tostring(plugin_id or "windrose-sidecar-bridge") .. "] nativeSpawn actor=" .. tostring(actor_name) .. " target=" .. tostring(target_label) .. " index=" .. tostring(i))
    else
      last_error = tostring(actor)
    end
  end

  if spawned > 0 then
    return true, "native-spawned-" .. tostring(spawned) .. "-" .. spec.name, spawned
  end
  return false, last_error or "SpawnActor returned no valid actors", spawned
end

local function game_thread_dispatch_state()
  local candidates = {
    "Z:\\home\\steam\\server-files\\R5\\Binaries\\Win64\\ue4ss\\UE4SS-settings.ini",
    "Z:\\home\\steam\\server-files\\UE4SS-settings.ini",
    ".\\UE4SS-settings.ini"
  }
  for _, path in ipairs(candidates) do
    local raw = read_file(path)
    if raw then
      local hook_engine_tick = raw:match("HookEngineTick%s*=%s*(%d)")
      local hook_process_event = raw:match("HookUObjectProcessEvent%s*=%s*(%d)")
      if hook_engine_tick == "1" or hook_process_event == "1" then
        return true, "settingsPath=" .. tostring(path) .. " HookEngineTick=" .. tostring(hook_engine_tick or "missing") .. " HookUObjectProcessEvent=" .. tostring(hook_process_event or "missing")
      end
      if hook_engine_tick == "0" and hook_process_event == "0" then
        return false, "settingsPath=" .. tostring(path) .. " HookEngineTick=0 HookUObjectProcessEvent=0"
      end
    end
  end
  return false, "settingsPath=not-found HookEngineTick=missing HookUObjectProcessEvent=missing"
end

local function game_thread_dispatch_enabled()
  local enabled = game_thread_dispatch_state()
  return enabled == true
end

local function create(context)
  local plugin_id = (context and context.plugin_id) or "windrose-sidecar-bridge"
  local mode = (context and context.mode) or "dry-run-only"
  local emit_error_event = assert(context and context.emit_error_event, "emit_error_event is required")
  local emit_player_readback_event = assert(context and context.emit_player_readback_event, "emit_player_readback_event is required")
  local write_action_result = assert(context and context.write_action_result, "write_action_result is required")

  local function ExecuteDodoSwarmNative(action)
    local pawn, target_label = resolve_target_pawn(action.targetPlayer, plugin_id)
    if not pawn then return false, target_label, 0 end

    local ok, outcome, spawned = spawn_creature_near_pawn(action, pawn, target_label, plugin_id)
    return ok, outcome, spawned or 0
  end

  local function HandleDodoSwarm(request)
    local target = request and request.targetPlayer or "unknown"
    local count = request and request.count or "unknown"
    local action_request_id = request and request.actionRequestId or "unknown"
    local creature_id = request and request.creatureId or "unknown"
    local creature_name = request and request.creatureName or "unknown"
    local dispatch_enabled, dispatch_detail = game_thread_dispatch_state()

    print("[" .. plugin_id .. "] actionLifecycle stage=handler-start actionRequestId=" .. tostring(action_request_id) .. " handler=HandleDodoSwarm target=" .. tostring(target) .. " creatureId=" .. tostring(creature_id) .. " creatureName=" .. tostring(creature_name) .. " count=" .. tostring(count) .. " mode=" .. tostring(mode) .. " gameThreadDispatch=" .. tostring(dispatch_enabled and "enabled" or "disabled") .. " " .. tostring(dispatch_detail))

    if mode ~= "dev-execute" then
      print("[" .. plugin_id .. "] denied HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=denied reason=plugin-mode-" .. tostring(mode))
      emit_error_event("plugin_mode_denied", "HandleDodoSwarm denied because plugin mode is not dev-execute.", action_request_id, nil, false)
      return false, "denied-plugin-mode", false
    end

    local pawn, target_label = resolve_target_pawn(request.targetPlayer, plugin_id)
    if not pawn then
      print("[" .. plugin_id .. "] actionLifecycle stage=target-lookup-failed actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " error=" .. tostring(target_label))
      emit_error_event("target_lookup_failed", tostring(target_label), action_request_id, nil, false)
      return false, target_label, false, 0
    end

    print("[" .. plugin_id .. "] actionLifecycle stage=target-resolved actionRequestId=" .. tostring(action_request_id) .. " requestedTarget=" .. tostring(target) .. " resolvedTarget=" .. tostring(target_label))
    emit_player_readback_event(tostring(target or target_label), tostring(target_label), true, action_request_id, "Read-only target lookup completed without mutating player state.")

    -- Prefer game-thread dispatch when UE4SS exposes it; UObject enumeration and
    -- SpawnActor are unsafe from the RCON/LoopAsync thread. The dispatched closure
    -- writes the result file itself, so the sidecar can poll for final readback.
    if type(ExecuteInGameThread) == "function" and type(write_action_result) == "function" and dispatch_enabled then
      print("[" .. plugin_id .. "] actionLifecycle stage=game-thread-submit actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " creatureId=" .. tostring(creature_id) .. " count=" .. tostring(count))
      ExecuteInGameThread(function()
        print("[" .. plugin_id .. "] actionLifecycle stage=game-thread-enter actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " creatureId=" .. tostring(creature_id) .. " count=" .. tostring(count))
        local ok, outcome, spawned = ExecuteDodoSwarmNative(request)
        if ok then
          print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=" .. tostring(outcome) .. " nativeSpawn=true spawnedCount=" .. tostring(spawned or 0))
          write_action_result(request, "executed", true, outcome or "native-spawned", "Plugin consumed the approved dev action and spawned native actor(s) on the game thread.", true, spawned or 0)
        else
          print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=native-spawn-failed nativeSpawn=false error=" .. tostring(outcome))
          emit_error_event("native_spawn_failed", tostring(outcome or "native-spawn-failed"), action_request_id, nil, true)
          write_action_result(request, "failed", false, outcome or "native-spawn-failed", "Plugin dispatched the native spawn action to the game thread, but it did not complete.", false, spawned or 0)
        end
      end)
      print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=native-spawn-scheduled")
      return true, "native-spawn-scheduled", false, 0, true
    end

    if not dispatch_enabled then
      print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=native-spawn-blocked-game-thread-dispatch-disabled nativeSpawn=false " .. tostring(dispatch_detail))
      emit_error_event("game_thread_dispatch_disabled", "Native spawn blocked because the game-thread dispatch gate is disabled. " .. tostring(dispatch_detail), action_request_id, nil, true)
      return false, "native-spawn-blocked-game-thread-dispatch-disabled", false, 0
    end

    -- Fallback path for builds where the game-thread dispatcher is unavailable
    -- even though hooks appear enabled.
    local ok, outcome, spawned = ExecuteDodoSwarmNative(request)

    if ok then
      print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=" .. tostring(outcome) .. " nativeSpawn=true spawnedCount=" .. tostring(spawned or 0))
      return true, outcome or "native-spawned", true, spawned or 0
    end

    print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=native-spawn-failed nativeSpawn=false error=" .. tostring(outcome))
    emit_error_event("native_spawn_failed", tostring(outcome or "native-spawn-failed"), action_request_id, nil, true)
    return false, outcome or "native-spawn-failed", false, spawned or 0
  end

  return HandleDodoSwarm
end

return {
  create = create
}
