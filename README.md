# Centinelas-de-la-Fortaleza

- Server-Client connection reworked
  - TCP removed
  - Timeouts implemented (missing reconnection)
  - Client data is now stored within a single struct per client instead of various member variables within ServerManager
- World replication
  - "Monigote" responds to user action across different devices (change state and speed)
- Server and Client connection are now independent from stored scene references and are now always loaded after connection is established
- Streamlined UI elements to use the same system (ModuleManager and InteractableModule)