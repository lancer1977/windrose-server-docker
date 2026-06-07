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

local function action_summary(action)
  if not action then
    return "actionRequestId=unknown actionId=unknown handler=unknown"
  end

  return "actionRequestId=" .. tostring(action.actionRequestId or "unknown") ..
    " actionId=" .. tostring(action.actionId or "unknown") ..
    " handler=" .. tostring(action.handler or "unknown") ..
    " target=" .. tostring(action.targetPlayer or "unknown") ..
    " creatureId=" .. tostring(action.creatureId or "unknown") ..
    " creatureName=" .. tostring(action.creatureName or "unknown") ..
    " count=" .. tostring(action.count or "unknown") ..
    " approvalId=" .. tostring(action.approvalId or "") ..
    " modeId=" .. tostring(action.modeId or "")
end

local function log_action(stage, action, fields)
  print("[" .. plugin_id .. "] actionLifecycle stage=" .. tostring(stage) .. " " .. action_summary(action) .. (fields and (" " .. fields) or ""))
end

local event_sequence = 0

local function next_event_id(prefix)
  event_sequence = event_sequence + 1
  return string.format("%s-%s-%04d", prefix or "event", os.date("%Y%m%d%H%M%S"), event_sequence)
end

local function write_bridge_event(message_type, body, message_id)
  mkdir_p(path_join(bridge_root, "events"))
  local event_id = message_id or next_event_id("event")
  local event_path = path_join(path_join(bridge_root, "events"), event_id .. ".json")
  if not write_file(event_path, body) then
    print("[" .. plugin_id .. "] failed to write bridge event type=" .. tostring(message_type) .. " path=" .. tostring(event_path))
    return false, event_id, event_path
  end
  return true, event_id, event_path
end

local function emit_heartbeat_event(status, notes)
  local message_id = next_event_id("heartbeat")
  local timestamp = now_utc()
  local body = string.format(
    '{"messageType":"windrose.heartbeat.v3","schemaVersion":"windrose.plugin_sidecar.v3","messageId":"%s","correlationId":"%s","originSurface":"WindrosePlus","targetSurface":"StateWeb","createdAtUtc":"%s","componentId":"%s","status":"%s","heartbeatAtUtc":"%s","notes":%s}\n',
    json_escape(message_id),
    json_escape(plugin_id .. ":" .. mode),
    json_escape(timestamp),
    json_escape(plugin_id),
    json_escape(status),
    json_escape(timestamp),
    notes and ('"' .. json_escape(notes) .. '"') or "null"
  )
  return write_bridge_event("windrose.heartbeat.v3", body, message_id)
end

local function emit_player_readback_event(player_id, display_name, is_online, correlation_id, notes)
  local message_id = next_event_id("player-readback")
  local timestamp = now_utc()
  local body = string.format(
    '{"messageType":"windrose.player.state.readback.v3","schemaVersion":"windrose.plugin_sidecar.v3","messageId":"%s","correlationId":%s,"originSurface":"WindrosePlus","targetSurface":"StateWeb","createdAtUtc":"%s","playerId":"%s","displayName":"%s","isOnline":%s,"observedAtUtc":"%s","location":null,"healthPercent":null,"staminaPercent":null,"lastKnownCommandId":null,"notes":%s}\n',
    json_escape(message_id),
    correlation_id and ('"' .. json_escape(correlation_id) .. '"') or "null",
    json_escape(timestamp),
    json_escape(player_id),
    json_escape(display_name),
    is_online and "true" or "false",
    json_escape(timestamp),
    notes and ('"' .. json_escape(notes) .. '"') or "null"
  )
  return write_bridge_event("windrose.player.state.readback.v3", body, message_id)
end

local function emit_error_event(error_code, message, correlation_id, related_message_id, retryable)
  local message_id = next_event_id("error")
  local body = string.format(
    '{"messageType":"windrose.error.v3","schemaVersion":"windrose.plugin_sidecar.v3","messageId":"%s","correlationId":"%s","originSurface":"WindrosePlus","targetSurface":"StateWeb","createdAtUtc":"%s","errorCode":"%s","message":"%s","isRetryable":%s,"relatedMessageId":%s}\n',
    json_escape(message_id),
    json_escape(correlation_id or (plugin_id .. ":" .. mode)),
    json_escape(now_utc()),
    json_escape(error_code),
    json_escape(message),
    retryable and "true" or "false",
    related_message_id and ('"' .. json_escape(related_message_id) .. '"') or "null"
  )
  return write_bridge_event("windrose.error.v3", body, message_id)
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
  mkdir_p(path_join(bridge_root, "events"))

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
  emit_heartbeat_event(mode == "dev-execute" and "healthy" or "healthy", mode == "dev-execute" and "plugin loaded; dev execution queue enabled; native actor spawn probe available" or "plugin loaded; dry-run native-hook seam available")
end

local HandleDodoSwarm

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

local function write_action_result(action, status, executed, outcome, message, native_spawn, spawned_count)
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
  local result_path = path_join(path_join(bridge_root, "results"), action_request_id .. ".json")
  local wrote = write_file(result_path, result)
  log_action("result-writeback", action, "status=" .. tostring(status) .. " executed=" .. tostring(executed and "true" or "false") .. " outcome=" .. tostring(outcome) .. " nativeSpawn=" .. tostring(native_spawn and "true" or "false") .. " spawnedCount=" .. tostring(tonumber(spawned_count) or 0) .. " resultPath=" .. tostring(result_path) .. " resultWriteOk=" .. tostring(wrote and "true" or "false"))
end

local function load_local_module(name)
  local ok, module = pcall(require, name)
  if ok then return module end

  local short_name = tostring(name):match("([^%.]+)$") or tostring(name)
  ok, module = pcall(require, short_name)
  if ok then return module end

  local source = debug and debug.getinfo and debug.getinfo(1, "S") and debug.getinfo(1, "S").source or ""
  local normalized_source = tostring(source):gsub("\\", "/")
  local script_dir = normalized_source:match("^@(.+/)init%.lua$")
  local candidate_paths = {}
  if script_dir then
    table.insert(candidate_paths, script_dir .. "modules/" .. short_name .. ".lua")
  end
  table.insert(candidate_paths, "Z:\\home\\steam\\server-files\\windrose_plus_mods\\windrose-sidecar-bridge\\modules\\" .. short_name .. ".lua")
  table.insert(candidate_paths, "/home/steam/server-files/windrose_plus_mods/windrose-sidecar-bridge/modules/" .. short_name .. ".lua")

  local errors = {}
  for _, path in ipairs(candidate_paths) do
    ok, module = pcall(dofile, path)
    if ok then return module end
    table.insert(errors, tostring(path) .. ": " .. tostring(module))
  end

  error("failed to load local module " .. tostring(name) .. ": " .. table.concat(errors, " | "))
end

local dodo_swarm = load_local_module("modules.dodo_swarm")
HandleDodoSwarm = dodo_swarm.create({
  plugin_id = plugin_id,
  mode = mode,
  emit_error_event = emit_error_event,
  emit_player_readback_event = emit_player_readback_event,
  write_action_result = write_action_result,
})

local function process_action(action_request_id)
  if not action_request_id or action_request_id == "" then return false end
  local action_path = path_join(path_join(bridge_root, "actions"), action_request_id .. ".json")
  log_action("dequeue", { actionRequestId = action_request_id }, "actionPath=" .. tostring(action_path))
  local body = read_file(action_path)
  if not body then
    write_action_result({ actionRequestId = action_request_id }, "failed", false, "action-file-missing", "Action file was listed in pending.txt but could not be read.")
    emit_error_event("action_file_missing", "Action file was listed in pending.txt but could not be read.", action_request_id, nil, true)
    return true
  end

  local action = action_from_body(body)
  action.actionRequestId = action.actionRequestId or action_request_id
  log_action("parsed", action, "approved=" .. tostring(action.approved) .. " dryRun=" .. tostring(action.dryRun))

  if action.approved ~= true or action.dryRun == true then
    log_action("denied", action, "reason=approval-required approved=" .. tostring(action.approved) .. " dryRun=" .. tostring(action.dryRun))
    write_action_result(action, "denied", false, "approval-required", "Queued action was not approved for dev execution.")
    emit_error_event("approval_required", "Queued action was not approved for dev execution.", action.actionRequestId, nil, false)
    return true
  end

  if action.actionId ~= "windrose.spawn.dodo_swarm" or action.handler ~= "HandleDodoSwarm" then
    log_action("denied", action, "reason=unsupported-action")
    write_action_result(action, "denied", false, "unsupported-action", "Only windrose.spawn.dodo_swarm/HandleDodoSwarm is allowed in V4.")
    emit_error_event("unsupported_action", "Only windrose.spawn.dodo_swarm/HandleDodoSwarm is allowed in V4.", action.actionRequestId, nil, false)
    return true
  end

  log_action("dispatch", action, "handler=HandleDodoSwarm")
  local ok, outcome, native_spawn, spawned_count, scheduled = HandleDodoSwarm(action)
  if scheduled then
    log_action("scheduled", action, "outcome=" .. tostring(outcome) .. " nativeSpawn=false")
    return true
  end
  if ok then
    log_action("handler-returned", action, "status=executed outcome=" .. tostring(outcome) .. " nativeSpawn=" .. tostring(native_spawn and "true" or "false") .. " spawnedCount=" .. tostring(spawned_count or 0))
    write_action_result(action, "executed", true, outcome or "native-spawned", "Plugin consumed the approved dev action and attempted native actor spawn.", native_spawn, spawned_count)
  else
    log_action("handler-returned", action, "status=failed outcome=" .. tostring(outcome) .. " nativeSpawn=" .. tostring(native_spawn and "true" or "false") .. " spawnedCount=" .. tostring(spawned_count or 0))
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
      if not ok then
        local error_message = tostring(processed)
        print("[" .. plugin_id .. "] actionLifecycle stage=processing-error actionRequestId=" .. tostring(trimmed) .. " error=" .. error_message)
        write_action_result({ actionRequestId = trimmed, actionId = "unknown", handler = "unknown" }, "failed", false, "action-processing-exception", error_message, false, 0)
        emit_error_event("action_processing_exception", error_message, trimmed, nil, true)
      elseif not processed then
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
  emit_error_event("queue_poller_unavailable", "WindrosePlus.API.registerTickCallback unavailable; action queue polling is disabled.", plugin_id .. ":" .. mode, nil, true)
end

return {
  pluginId = plugin_id,
  mode = mode,
  maxTeleportersPerIsland = max_teleporters_per_island,
  requestedStackSizeMultiplier = requested_stack_size_multiplier,
  HandleDodoSwarm = HandleDodoSwarm,
  ProcessPendingActions = process_pending_actions
}
