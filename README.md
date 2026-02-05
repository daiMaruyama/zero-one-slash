# zero-one-slash

Reflex-based darts checkout trainer.

## Summary
Zero-one-slash is a reflex-focused darts checkout trainer where players clear randomly selected target scores under a time limit.
Success depends on quick decision-making, accurate throws, and efficient score management.

## How to Play
1. A target score is displayed at the start of each turn.
2. Use up to three throws to reduce the target to exactly zero.
3. If the score goes below zero, the turn ends as a bust.
4. Clear the target to earn bonus points, then proceed to the next target.

## Rules and Scoring
- Clears grant bonus points based on finishing area.
- Misses and busts advance the turn after a short delay.
- The game ends when the timer reaches zero and the result panel is shown.

## Controls
- Mouse or touch input to throw.
- Optional debug name entry can be opened with the configured key (default: F2).

## Development
- Engine: Unity
- Main scene: open the project in Unity and run the primary scene from the editor.

## Project Structure
- `Assets/Scripts/Audio`: audio playback and volume control.
- `Assets/Scripts/Camera`: camera movement and shake.
- `Assets/Scripts/Core`: gameplay logic and board systems.
- `Assets/Scripts/Effects`: hit effects, bloom, and popups.
- `Assets/Scripts/Rendering`: URP volume helpers.
- `Assets/Scripts/UI`: UI controllers and UI effects.
