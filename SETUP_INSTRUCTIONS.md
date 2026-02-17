# Button Controlled Sprite Player Setup Guide

## Overview
This project has been simplified to work with 3 SpriteFramePlayers controlled by MobileControllerFramework buttons.

## Component Created
**ButtonControlledSpritePlayer.cs** - Located in `Assets/Scripts/Core/Flow/`

This component manages 3 SpriteFramePlayers:
1. **Idle Player** - Plays automatically when the scene starts (loops by default)
2. **Action Player 1** - Plays when Button 1 is pressed
3. **Action Player 2** - Plays when Button 2 is pressed

## Scene Setup Instructions

### 1. Open the Game Scene
- Open `Assets/Scenes/Game.unity` in Unity

### 2. Create GameObjects for Each Sprite Frame Player

Create 3 empty GameObjects:
1. Right-click in Hierarchy → Create Empty → Name it "IdlePlayer"
2. Right-click in Hierarchy → Create Empty → Name it "Action1Player"
3. Right-click in Hierarchy → Create Empty → Name it "Action2Player"

For each GameObject:
- Add Component → `Sprite Renderer`
- Add Component → `Sprite Frame Player` (from SickOfIt.VideoFrameExtractor.Runtime)

### 3. Setup Your Sprite Frame Data

For each SpriteFramePlayer:
1. Create or assign a `SpriteFramesData` asset:
   - Right-click in Project → Create → Video → Sprite Frames Data
   - Configure your video frames using the Sprite Frames Manager (Tools → Sprite Frames Manager)

2. In the SpriteFramePlayer component:
   - Assign the `Sprite Frames Data` field
   - Set `Preload On Awake` to **true**
   - For Idle Player: Set `Loop` to **true**
   - For Action Players: Set `Loop` to **false** (so they return to idle after playing)

### 4. Create MobileControllerFramework Buttons

Create 2 buttons using the MobileControllerFramework:

**Button 1:**
1. Right-click in Hierarchy → UI → Button (or use MobileControllerFramework button prefab)
2. Add Component → `MobileControllerFramework.Button` if not already present
3. Position it where you want (e.g., bottom-left of screen)
4. Add a Text/TextMeshPro child to label it (optional)

**Button 2:**
1. Repeat the same process
2. Position it differently (e.g., bottom-right of screen)

### 5. Setup the Controller

1. Create an empty GameObject in the Hierarchy
2. Name it "SpritePlayerController"
3. Add Component → `Button Controlled Sprite Player`

4. Assign references in the Inspector:
   - **Idle Player Behaviour**: Drag the "IdlePlayer" GameObject
   - **Action1 Player Behaviour**: Drag the "Action1Player" GameObject
   - **Action2 Player Behaviour**: Drag the "Action2Player" GameObject
   - **Button1**: Drag your first button GameObject
   - **Button2**: Drag your second button GameObject

5. Configure settings:
   - **Return To Idle After Action**: Check this if you want the idle animation to resume after an action completes
   - **Hide Idle When Playing Action**: Check this to hide the idle player when actions play (recommended: **true**)

## How It Works

1. When the scene starts, the **Idle Player** will automatically start playing and loop continuously
2. When the user presses **Button 1**, the Idle Player is hidden and **Action1 Player** starts playing
3. When the user presses **Button 2**, the Idle Player is hidden and **Action2 Player** starts playing
4. When an action player finishes:
   - If "Return To Idle After Action" is enabled: Idle Player resumes
   - If disabled: Nothing plays until another button is pressed

## Important Notes

- All SpriteFramePlayers should have `Preload On Awake` enabled for instant playback
- The Idle Player should have `Loop` enabled
- Action Players should have `Loop` disabled (unless you want them to loop forever)
- Make sure your SpriteFramesData assets are properly configured with video frames
- The component automatically waits for players to load before playing them

## Troubleshooting

**Problem:** Nothing plays
- Check that all SpriteFramePlayers have valid SpriteFramesData assigned
- Check that Preload On Awake is enabled
- Check the Console for warning messages

**Problem:** Buttons don't work
- Make sure the Button references are assigned in the ButtonControlledSpritePlayer component
- Check that the buttons have the MobileControllerFramework.Button component
- Check that the buttons are not disabled or hidden

**Problem:** Action players don't show
- Check that the GameObjects are active in the Hierarchy
- Check that "Hide Idle When Playing Action" is enabled if you want to hide the idle player
- Check that the action players are positioned correctly (same position as idle player, or adjust as needed)

## Example Workflow

1. Create your video frames (or sprite sequences)
2. Use the Sprite Frames Manager (Tools → Sprite Frames Manager) to pack them into sprite sheets
3. Create SpriteFramesData assets for each animation (idle, action1, action2)
4. Follow the setup instructions above
5. Test in Play mode - idle should play automatically, buttons should trigger actions

## Next Steps

You can extend this system by:
- Adding more action players and buttons (modify the component)
- Adding sound effects when buttons are pressed
- Adding UI feedback when actions complete
- Creating different animation states or combo systems
