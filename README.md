
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


# v0.1

## Contributors & Tasks

### Pau Mena Torres
- Import Lab2 Base
- JSON serialization and deserialization
- UI interactable objects
- Improve client and server connection
- Bug fixing
- Presentation

### Edgar Mesa Domínguez
- Import Lab2 Base
- JSON serialization and deserialization
- UI interactable objects
- Improve client and server connection
- Presentation
- Bug Fixing

### Roger Puchol Celma
- Module Manager implementation
- Interactable Module and derivatives

### Èric Palomares Rodríguez
- Presentation

## Next Steps
- Change serialization method to a custom serializer that filters unnecessary data.
- Change protocol method to mix them.
- Implement game mechanics
- Continue improving UX and UI.

## Known issues
- Slider bar serialization may not be working as expected in some cases. Hotfix planned.


