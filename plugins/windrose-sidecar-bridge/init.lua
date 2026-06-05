-- Windrose Sidecar Bridge
--
-- V4 provides the dev-only command queue/readback boundary plus a guarded
-- native actor-spawn probe for approved Windrose dev-server smoke tests.

local function normalize_bridge_root(path)
  local normalized = tostring(path or "windrose_plugin_bridge")
  if normalized:sub(1, 12) == "/home/steam/" then
    normalized = "Z:" .. normalized:gsub("/", "\\")
  end
  return normalized
end

local plugin_id = "windrose-sidecar-bridge"
local bridge_root = normalize_bridge_root(os.getenv("WINDROSE_PLUGIN_BRIDGE_PATH") or "Z:\\home\\steam\\server-files\\windrose_plugin_bridge")
local sidecar_url = os.getenv("WINDROSE_STATE_WEB_URL") or "http://windrose-state-web:8781"
local mode = os.getenv("WINDROSE_SIDECAR_PLUGIN_MODE") or "dry-run-only"
local max_teleporters_per_island = tonumber(os.getenv("WINDROSE_MAX_TELEPORTERS_PER_ISLAND") or "3") or 3
local requested_stack_size_multiplier = tonumber(os.getenv("WINDROSE_REQUESTED_STACK_SIZE_MULTIPLIER") or "1") or 1

local function shell_quote(value)
  return "'" .. tostring(value):gsub("'", "'\\''") .. "'"
end

local function mkdir_p(path)
  os.execute("mkdir -p " .. shell_quote(path))
end

local function write_file(path, body)
  local file, err = io.open(path, "w")
  if not file then
    print("[" .. plugin_id .. "] failed to open " .. path .. ": " .. tostring(err))
    return false
  end

  file:write(body)
  file:close()
  return true
end

local function now_utc()
  return os.date("!%Y-%m-%dT%H:%M:%SZ")
end

local function path_join(root, leaf)
  local sep = root:find("\\", 1, true) and "\\" or "/"
  return root .. sep .. leaf
end

local function read_file(path)
  local file = io.open(path, "r")
  if not file then
    return nil
  end

  local body = file:read("*a")
  file:close()
  return body
end

local function json_escape(value)
  return tostring(value or "")
    :gsub('\\', '\\\\')
    :gsub('"', '\\"')
    :gsub('\n', '\\n')
    :gsub('\r', '\\r')
end

local function json_string(body, key)
  if not body then
    return nil
  end

  local pattern = '"' .. key .. '"%s*:%s*"([^"]*)"'
  return body:match(pattern)
end

local function json_bool(body, key)
  if not body then
    return nil
  end

  local pattern = '"' .. key .. '"%s*:%s*(true)'
  if body:match(pattern) then return true end
  pattern = '"' .. key .. '"%s*:%s*(false)'
  if body:match(pattern) then return false end
  return nil
end

local function json_number(body, key)
  if not body then
    return nil
  end

  local pattern = '"' .. key .. '"%s*:%s*(%d+)'
  return tonumber(body:match(pattern))
end

local bridge_config = read_file(path_join(bridge_root, "config.json"))
mode = json_string(bridge_config, "mode") or mode
max_teleporters_per_island = json_number(bridge_config, "maxTeleportersPerIsland") or max_teleporters_per_island
requested_stack_size_multiplier = json_number(bridge_config, "requestedStackSizeMultiplier") or requested_stack_size_multiplier

local write_action_result

local function write_status(message)
  mkdir_p(bridge_root)
  mkdir_p(path_join(bridge_root, "actions"))
  mkdir_p(path_join(bridge_root, "results"))

  local status = string.format(
    '{"pluginId":"%s","status":"started","startedAt":"%s","sidecarUrl":"%s","mode":"%s","limits":{"maxTeleportersPerIsland":%d,"requestedStackSizeMultiplier":%d,"stackSizeEnforcement":"disabled-upstream-no-live-write"},"capabilities":{"nativeActorSpawn":%s,"approvedDevExecutionOnly":true},"message":"%s"}\n',
    plugin_id,
    now_utc(),
    sidecar_url,
    mode,
    max_teleporters_per_island,
    requested_stack_size_multiplier,
    mode == "dev-execute" and "true" or "false",
    message or "heartbeat written; live execution disabled"
  )

  write_file(path_join(bridge_root, "status.json"), status)
end

local creature_specs = {
  ["R5.Creature.Dodo"] = { name = "Dodo", assetPath = "/Game/Gameplay/Character/AI/Mob/Dodo/BP_Mob_Dodo", assetName = "BP_Mob_Dodo" },
  ["R5.Creature.Wolf"] = { name = "Wolf", assetPath = "/Game/Gameplay/Character/AI/Mob/Wolf/BP_Mob_Wolf", assetName = "BP_Mob_Wolf" }
}

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

local function resolve_target_pawn(target)
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
      print("[" .. plugin_id .. "] target lookup used WindrosePlus Admin single-online-pawn fallback requestedTarget=" .. tostring(target))
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
    print("[" .. plugin_id .. "] target lookup used single-online-pawn fallback requestedTarget=" .. tostring(target) .. " resolved=" .. tostring(fallback_label))
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

local function spawn_creature_near_pawn(action, pawn, target_label)
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
      print("[" .. plugin_id .. "] nativeSpawn actor=" .. tostring(actor_name) .. " target=" .. tostring(target_label) .. " index=" .. tostring(i))
    else
      last_error = tostring(actor)
    end
  end

  if spawned > 0 then
    return true, "native-spawned-" .. tostring(spawned) .. "-" .. spec.name, spawned
  end
  return false, last_error or "SpawnActor returned no valid actors", spawned
end

local function ExecuteDodoSwarmNative(action)
  local pawn, target_label = resolve_target_pawn(action.targetPlayer)
  if not pawn then return false, target_label, 0 end

  local ok, outcome, spawned = spawn_creature_near_pawn(action, pawn, target_label)
  return ok, outcome, spawned or 0
end

local function game_thread_dispatch_enabled()
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
      if hook_engine_tick == "1" or hook_process_event == "1" then return true end
      if hook_engine_tick == "0" and hook_process_event == "0" then return false end
    end
  end
  return false
end

local function HandleDodoSwarm(request)
  local target = request and request.targetPlayer or "unknown"
  local count = request and request.count or "unknown"
  local action_request_id = request and request.actionRequestId or "unknown"

  if mode ~= "dev-execute" then
    print("[" .. plugin_id .. "] denied HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=denied reason=plugin-mode-" .. tostring(mode))
    return false, "denied-plugin-mode", false
  end

  -- Prefer game-thread dispatch when UE4SS exposes it; UObject enumeration and
  -- SpawnActor are unsafe from the RCON/LoopAsync thread. The dispatched closure
  -- writes the result file itself, so the sidecar can poll for final readback.
  if type(ExecuteInGameThread) == "function" and type(write_action_result) == "function" and game_thread_dispatch_enabled() then
    ExecuteInGameThread(function()
      local ok, outcome, spawned = ExecuteDodoSwarmNative(request)
      if ok then
        print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=" .. tostring(outcome) .. " nativeSpawn=true spawnedCount=" .. tostring(spawned or 0))
        write_action_result(request, "executed", true, outcome or "native-spawned", "Plugin consumed the approved dev action and spawned native actor(s) on the game thread.", true, spawned or 0)
      else
        print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=native-spawn-failed nativeSpawn=false error=" .. tostring(outcome))
        write_action_result(request, "failed", false, outcome or "native-spawn-failed", "Plugin dispatched the native spawn action to the game thread, but it did not complete.", false, spawned or 0)
      end
    end)
    print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=native-spawn-scheduled")
    return true, "native-spawn-scheduled", false, 0, true
  end

  if not game_thread_dispatch_enabled() then
    print("[" .. plugin_id .. "] dev-execute HandleDodoSwarm actionRequestId=" .. tostring(action_request_id) .. " target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=native-spawn-blocked-game-thread-dispatch-disabled nativeSpawn=false")
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
  return false, outcome or "native-spawn-failed", false, spawned or 0
end

local function action_from_body(body)
  return {
    actionRequestId = json_string(body, "actionRequestId"),
    actionId = json_string(body, "actionId"),
    handler = json_string(body, "handler"),
    targetPlayer = json_string(body, "targetPlayer"),
    approvalId = json_string(body, "approvalId"),
    modeId = json_string(body, "modeId"),
    creatureId = json_string(body, "creatureId"),
    creatureName = json_string(body, "creatureName"),
    count = json_number(body, "count"),
    radiusMeters = json_number(body, "radiusMeters"),
    offsetMeters = json_number(body, "offsetMeters"),
    approved = json_bool(body, "approved"),
    dryRun = json_bool(body, "dryRun")
  }
end

function write_action_result(action, status, executed, outcome, message, native_spawn, spawned_count)
  mkdir_p(path_join(bridge_root, "results"))
  local action_request_id = action and action.actionRequestId or "unknown"
  local result = string.format(
    '{"pluginId":"%s","actionRequestId":"%s","actionId":"%s","handler":"%s","status":"%s","executed":%s,"dryRun":false,"outcome":"%s","message":"%s","targetPlayer":"%s","approvalId":"%s","modeId":"%s","creatureId":"%s","creatureName":"%s","observedAt":"%s","nativeSpawn":%s,"spawnedCount":%d}\n',
    plugin_id,
    json_escape(action_request_id),
    json_escape(action and action.actionId or ""),
    json_escape(action and action.handler or ""),
    json_escape(status),
    executed and "true" or "false",
    json_escape(outcome),
    json_escape(message),
    json_escape(action and action.targetPlayer or ""),
    json_escape(action and action.approvalId or ""),
    json_escape(action and action.modeId or ""),
    json_escape(action and action.creatureId or ""),
    json_escape(action and action.creatureName or ""),
    now_utc(),
    native_spawn and "true" or "false",
    tonumber(spawned_count) or 0
  )
  write_file(path_join(path_join(bridge_root, "results"), action_request_id .. ".json"), result)
end

local function process_action(action_request_id)
  if not action_request_id or action_request_id == "" then return false end
  local action_path = path_join(path_join(bridge_root, "actions"), action_request_id .. ".json")
  local body = read_file(action_path)
  if not body then
    write_action_result({ actionRequestId = action_request_id }, "failed", false, "action-file-missing", "Action file was listed in pending.txt but could not be read.")
    return true
  end

  local action = action_from_body(body)
  action.actionRequestId = action.actionRequestId or action_request_id

  if action.approved ~= true or action.dryRun == true then
    write_action_result(action, "denied", false, "approval-required", "Queued action was not approved for dev execution.")
    return true
  end

  if action.actionId ~= "windrose.spawn.dodo_swarm" or action.handler ~= "HandleDodoSwarm" then
    write_action_result(action, "denied", false, "unsupported-action", "Only windrose.spawn.dodo_swarm/HandleDodoSwarm is allowed in V4.")
    return true
  end

  local ok, outcome, native_spawn, spawned_count, scheduled = HandleDodoSwarm(action)
  if scheduled then
    return true
  end
  if ok then
    write_action_result(action, "executed", true, outcome or "native-spawned", "Plugin consumed the approved dev action and attempted native actor spawn.", native_spawn, spawned_count)
  else
    write_action_result(action, "failed", false, outcome or "not-executed", "Plugin did not complete the queued native spawn action.", native_spawn, spawned_count)
  end

  return true
end

local function process_pending_actions()
  local actions_root = path_join(bridge_root, "actions")
  local pending_path = path_join(actions_root, "pending.txt")
  local pending = read_file(pending_path)
  if not pending or pending == "" then return false end

  local remaining = {}
  for action_request_id in pending:gmatch("[^\r\n]+") do
    local trimmed = tostring(action_request_id):match("^%s*(.-)%s*$")
    if trimmed ~= "" then
      local ok, processed = pcall(process_action, trimmed)
      if not ok or not processed then
        table.insert(remaining, trimmed)
      end
    end
  end

  write_file(pending_path, table.concat(remaining, "\n") .. (#remaining > 0 and "\n" or ""))
  return true
end

write_status(mode == "dev-execute" and "plugin loaded; dev execution queue enabled; native actor spawn probe available" or "plugin loaded; dry-run native-hook seam available")
print("[" .. plugin_id .. "] loaded in " .. mode .. " mode; sidecar=" .. sidecar_url .. "; bridgeRoot=" .. bridge_root)
print("[" .. plugin_id .. "] policy maxTeleportersPerIsland=" .. tostring(max_teleporters_per_island) .. " enforcement=contract-only")
print("[" .. plugin_id .. "] policy requestedStackSizeMultiplier=" .. tostring(requested_stack_size_multiplier) .. " enforcement=disabled-upstream-no-live-write")

pcall(process_pending_actions)
if WindrosePlus and WindrosePlus.API and type(WindrosePlus.API.registerTickCallback) == "function" then
  WindrosePlus.API.registerTickCallback(function()
    pcall(process_pending_actions)
  end, 2000)
  print("[" .. plugin_id .. "] registered action queue poller intervalMs=2000")
else
  print("[" .. plugin_id .. "] action queue poller not registered; WindrosePlus.API.registerTickCallback unavailable")
end

return {
  pluginId = plugin_id,
  mode = mode,
  maxTeleportersPerIsland = max_teleporters_per_island,
  requestedStackSizeMultiplier = requested_stack_size_multiplier,
  HandleDodoSwarm = HandleDodoSwarm,
  ProcessPendingActions = process_pending_actions
}
