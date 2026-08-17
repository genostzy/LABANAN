# LABANAN Unity - Online Multiplayer Fighting Game

## Quick Start

### 1. Open in Unity
1. Open Unity Hub
2. Click **Add** > **Add project from disk**
3. Select the `LABANAN_Unity` folder
4. Open the project (Unity 2022 LTS or newer recommended)

### 2. Import Original Assets
Copy these files from the Java project into the Unity project:

```
From Java: res/                    → Unity: Assets/UI/
From Java: resFX/                  → Unity: Assets/Audio/
From Java: res/RED_SPRITESHEET.png → Unity: Assets/Sprites/Red/
From Java: res/BLUE_SPRITESHEET.png→ Unity: Assets/Sprites/Blue/
```

### 3. Create Scenes
Create these scenes in Unity:
1. **MainMenu** (Assets/Scenes/MainMenu.unity)
2. **OnlineLobby** (Assets/Scenes/OnlineLobby.unity)
3. **Game** (Assets/Scenes/Game.unity)

### 4. Setup Game Scene
In the **Game** scene:

1. Create empty GameObject named "GameLoop" and add `GameLoop.cs`
2. Create empty GameObject named "NetworkManager" and add `NetworkManager.cs`
3. Create empty GameObject named "AudioManager" and add `AudioManager.cs`
4. Create empty GameObject named "UIManager" and add `UIManager.cs`
5. Create empty GameObject named "GameManager" and add `GameManager.cs`

### 5. Setup UI
Create a Canvas and add:
- Health bars (Image components with Filled type)
- Timer text
- Round text
- Score texts
- Overlays (LABAN, Pause, Game Over)
- Connection UI (ping, rollback indicator)

### 6. Setup Player Sprites
1. Import sprite sheets into Unity
2. Use **Sprite Editor** to slice (64x64 grid, 20 rows x 12 columns)
3. Create **Animator Controllers** for Red and Blue players
4. Add all 20 animation states

### 7. Run
1. Build settings > Add all 3 scenes
2. Press Play
3. One player creates room, shares IP
4. Other player joins with IP
5. Fight!

## Controls

### Player 1 (Red)
| Action | Key |
|--------|-----|
| Move Left | A |
| Move Right | D |
| Jump | W |
| Crouch | S |
| Sword Attack | C |
| Sungkit Attack | V |
| Launch Attack | E |
| Block | Q (hold) |

### Player 2 (Blue)
| Action | Key |
|--------|-----|
| Move Left | Left Arrow |
| Move Right | Right Arrow |
| Jump | Up Arrow |
| Crouch | Down Arrow |
| Sword Attack | Numpad 1 |
| Sungkit Attack | Numpad 2 |
| Launch Attack | Numpad 3 |
| Block | Numpad 0 (hold) |

### System
| Action | Key |
|--------|-----|
| Pause | ESC |
| Debug Info | H |

## Network Architecture

- **Protocol**: UDP (direct IP connection)
- **Netcode**: Rollback with 2-frame input delay
- **Architecture**: Peer-to-peer (no server needed)
- **Tick Rate**: 60 Hz

## Game Rules

- 4 rounds max
- First to 2 round wins takes the game
- 60-second timer per round
- 500 HP per player
- 10 damage per attack
- Death zone at bottom (instant kill)

## File Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── FixedMath.cs        - Fixed-point math
│   │   ├── GameManager.cs      - Round/timer logic
│   │   ├── GameLoop.cs         - Main game loop
│   │   ├── PlatformManager.cs  - Platform collision
│   │   └── AudioManager.cs     - Audio playback
│   ├── Player/
│   │   ├── PlayerController.cs - Movement/combat
│   │   └── PlayerState.cs      - Serializable state
│   ├── Network/
│   │   ├── NetworkManager.cs   - UDP + rollback
│   │   ├── RollbackManager.cs  - State snapshots
│   │   ├── GameState.cs        - Full game snapshot
│   │   ├── InputData.cs        - Input serialization
│   │   └── LobbyManager.cs     - Room creation
│   └── UI/
│       ├── UIManager.cs        - In-game UI
│       ├── MainMenuUI.cs       - Main menu
│       ├── LobbyUI.cs          - Online lobby
│       └── ConnectionUI.cs     - Connection info
├── Scenes/
│   ├── MainMenu.unity
│   ├── OnlineLobby.unity
│   └── Game.unity
├── Sprites/
│   ├── Red/
│   └── Blue/
├── UI/
└── Audio/
```
