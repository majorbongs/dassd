local isWalkingStyleLocked = false

---@param toggle boolean
exports('SetWalkingStyleLocked', function(toggle)
  isWalkingStyleLocked = toggle
end)

function WalkMenuStart(name)
  if isWalkingStyleLocked then
    return
  end

  TriggerServerEvent("gtacnr:setWalkingStyle", name)
  RequestWalking(name)
  SetPedMovementClipset(PlayerPedId(), name, 0.2)
  RemoveAnimSet(name)
end

function RequestWalking(set)
  RequestAnimSet(set)
  while not HasAnimSetLoaded(set) do
    Citizen.Wait(1)
  end
end

function WalksOnCommand(source, args, raw)
  local WalksCommand = ""
  for a in pairsByKeys(DP.Walks) do
    WalksCommand = WalksCommand .. ""..string.lower(a)..", "
  end
  EmoteChatMessage(WalksCommand)
  EmoteChatMessage("To reset do /walk reset")
end

function WalkCommandStart(source, args, raw)
  if isWalkingStyleLocked then
    return
  end

  local name = firstToUpper(args[1])

  if name == "Reset" then
      TriggerServerEvent("gtacnr:resetWalkingStyle")
      ResetPedMovementClipset(PlayerPedId()) return
  end

  local name2 = table.unpack(DP.Walks[name])
  if name2 ~= nil then
    WalkMenuStart(name2)
  else
    EmoteChatMessage("'"..name.."' is not a valid walk")
  end
end