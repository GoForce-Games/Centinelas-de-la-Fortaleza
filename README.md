
# Centinelas-de-la-Fortaleza
Centinelas de la Fortaleza is a game where you will receive waves of enemies at your castle, and your objective is to defend and repair the castle.

To achieve this, players must be highly synchronized, as actions that need to be completed within a time limit will appear on their screens, such as "Repair the drawbridge." One player will receive the order and must communicate it to all players so it can be resolved. The orders that appear on one player's screen must be activated on the screen of the player who has that action available.

- Gameplay implementation redone
  - Game is now competitive and co-op
  - 4 players max
  - All players see the same set of tasks
  - First player to complete one gets points
  - If too many tasks go uncompleted the game is lost
  - Highest contributor wins
- World replication
  - "Monigote" responds to user action across different devices (make it walk around, stop, etc. All synced across devices)
  - Each player gets its own character that reacts to them completing or failing tasks
  - Individual task modules are reflected on every player's screen
- Server and Client connection are now independent from stored scene references and are now always loaded after connection is established
- UDP improvements
  - Packets are now acknowledge by the receiver and resent if not acknowledged within a certain time window
  - Less actions are delegated to the main thread
- Reworked UI flow from game launch to game start
  - Separated lobby scene from gameplay scene


# v1.0

## Contributors & Tasks

### Pau Mena Torres
- UDP connection refactor
- Gameplay implementation
- World replication fixes
- UI
- Bug fixing

### Edgar Mesa Domínguez
- UDP connection refactor
- Gameplay implementation
- World replication fixes
- UI
- Bug fixing

### Roger Puchol Celma
- UDP connection refactor
- UDP packet reliability
- World replication system
- Packet timeout
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
- UI doesn't scale properly (16:9 for best experience)
- Some buttons are covered by invisible UI elements, making it harder to click them

