
# Centinelas-de-la-Fortaleza
Centinelas de la Fortaleza is a game where you will receive waves of enemies at your castle, and your objective is to defend and repair the castle.

To achieve this, players must be highly synchronized, as actions that need to be completed within a time limit will appear on their screens, such as "Repair the drawbridge." One player will receive the order and must communicate it to all players so it can be resolved. The orders that appear on one player's screen must be activated on the screen of the player who has that action available.

- Server-Client connection reworked
  - TCP removed
  - Timeouts implemented (missing reconnection)
  - Client data is now stored within a single struct per client instead of various member variables within ServerManager
- World replication
  - "Monigote" responds to user action across different devices (change state and speed)
- Server and Client connection are now independent from stored scene references and are now always loaded after connection is established
- Streamlined UI elements to use the same system (ModuleManager and InteractableModule)


# v1.0

## Contributors & Tasks

### Pau Mena Torres
- UDP connection refactor
- Gameplay implementation
- World replication fixes
- UI

### Edgar Mesa Domínguez
- UDP connection refactor
- Gameplay implementation
- World replication fixes
- UI

### Roger Puchol Celma
- UDP connection refactor
- UDP packet reliability
- Packet timeout
- World replication system
- Bug fixing

### Èric Palomares Rodríguez
- UDP packet reliability
- UDP connection refactor
- Presentation

## Next Steps
- Reduce packet frequency (too many packets per second compared to game update needs)
- Optimize package size
- Cleanup code

## Known issues
- UI images don't scale properly

