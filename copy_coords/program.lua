local formats = {
    ["normal"] = { "X, Y, Z", "X, Y, Z, H" },
    ["nospaces"] = { "X,Y,Z", "X,Y,Z,H" },
    ["curly"] = { "{ X, Y, Z }", "{ X, Y, Z, H }" },
    ["square"] = { "[ X, Y, Z ]", "[ X, Y, Z, H ]" },
    ["xyzh"] = { "x = X, y = Y, z = Z", "x = X, y = Y, z = Z, h = H" },
    ["xyzh_curly"] = { "{ x = X, y = Y, z = Z }", "{ x = X, y = Y, z = Z, h = H }" },
    ["xyzh_square"] = { "[ x = X, y = Y, z = Z ]", "[ x = X, y = Y, z = Z, h = H ]" },
    ["csharp"] = { "Xf, Yf, Zf", "Xf, Yf, Zf, Hf" },
    ["csharp_curly"] = { "{ Xf, Yf, Zf }", "{ Xf, Yf, Zf, Hf }" },
    ["csharp_square"] = { "[ Xf, Yf, Zf ]", "[ Xf, Yf, Zf, Hf ]" }
}

local format = formats["normal"]

RegisterCommand("copycoordsformat", function(source, args, raw)
    local formatId = args[1]
    format = formats[formatId]
    print("Format was set to " .. formatId)
end, false)

RegisterCommand("copycoords", function() 
    local ped = PlayerPedId()
    local coords = GetEntityCoords(ped)

    if IsPedInAnyVehicle(ped) then 
        local vehicle = GetVehiclePedIsIn(ped)
        coords = GetEntityCoords(vehicle)
    end

    local message = {
        type = "copyCoords",
        format = format[1],
        x = coords.x,
        y = coords.y,
        z = coords.z
    }
    SendNuiMessage(json.encode(message))
end, false)


RegisterCommand("copycoordsandheading", function() 
    print("copycoordsandheading was renamed to copycoordsh")
end, false)

RegisterCommand("copycoordsh", function() 
    local ped = PlayerPedId()
    local coords = GetEntityCoords(ped)
    local heading = GetEntityHeading(ped)
    
    if IsPedInAnyVehicle(ped) then 
        local vehicle = GetVehiclePedIsIn(ped)
        coords = GetEntityCoords(vehicle)
        heading = GetEntityHeading(vehicle)
    end

    local message = {
        type = "copyCoordsAndHeading",
        format = format[2],
        x = coords.x,
        y = coords.y,
        z = coords.z,
        h = heading
    }
    SendNuiMessage(json.encode(message))
end, false)
