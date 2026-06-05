-- Windrose Sidecar Bridge
--
-- This is intentionally a dry-run plugin skeleton. It proves the WindrosePlus
-- load path and sidecar heartbeat/contract boundary without mutating live game
-- state. Real dodo spawning still needs a proven native hook before this module
-- may execute anything against the server.

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

local function write_status(message)
  mkdir_p(bridge_root)
  mkdir_p(path_join(bridge_root, "actions"))
  mkdir_p(path_join(bridge_root, "results"))

  local status = string.format(
    '{"pluginId":"%s","status":"started","startedAt":"%s","sidecarUrl":"%s","mode":"%s","message":"%s"}\n',
    plugin_id,
    now_utc(),
    sidecar_url,
    mode,
    message or "heartbeat written; live execution disabled"
  )

  write_file(path_join(bridge_root, "status.json"), status)
end

local function HandleDodoSwarm(request)
  -- Native-hook placeholder only. A future implementation must resolve a
  -- target player, validate limits, require approval, and then call a proven
  -- Windrose native spawn hook. This function deliberately does not spawn.
  local target = request and request.targetPlayer or "unknown"
  local count = request and request.count or "unknown"
  print("[" .. plugin_id .. "] dry-run HandleDodoSwarm target=" .. tostring(target) .. " count=" .. tostring(count) .. " result=not-executed approvalRequired=true")
  return false
end

write_status("plugin loaded; dry-run native-hook seam available")
print("[" .. plugin_id .. "] loaded in " .. mode .. " mode; sidecar=" .. sidecar_url .. "; bridgeRoot=" .. bridge_root)

return {
  pluginId = plugin_id,
  mode = mode,
  HandleDodoSwarm = HandleDodoSwarm
}
