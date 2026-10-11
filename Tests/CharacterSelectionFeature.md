# Character color selection

MultiplayerGame uses WaitingForPlayers as its selection phase. Movement, attacks and traps stay gated.
NetworkMatchManager stores ten replicated color owners and ready flags. Only the server changes them.
Clicks reserve a color; Ready locks it. Change Color clears ready, retaining the reservation until another pick succeeds.
Concurrent picks are serialized on the server: one wins, others keep their old color and receive a rejection message.
RPC checks use the real sender, active-player membership, a valid player object, valid index and waiting match state.
Disconnects free slots. Every connected player must select and confirm before the normal countdown starts.
Restart resets reservations. Selected colors become DamageFlash base colors so damage flashes restore correctly.

The runtime UI uses the existing font/canvas with a responsive 5x2 grid, hover scaling, selected white corner brackets,
and a brief diagonal white gradient masked inside the circle. Taken slots are dimmed, disabled and labelled PICKED.
The ready counter includes all connected players, including those still loading. The invisible server has no selection UI.

Palette: FFFFFF, ECFF00, 88FF00, 00FFAB, 00ABFF, AFB7FF, F281FF, FF9728, 8C8C8C, FF5C5C.

## Live acceptance tests

1. Start a server + two clients, then Start Game: both see selection. No movement, attacks, traps or countdown yet.
2. Hover: circle smoothly expands without moving the layout. Pick: shimmer stays inside circle and white corners appear.
3. Simultaneously select the same color: exactly one wins. Loser sees PICKED/message; previous selection is preserved.
4. Ready one player: game waits. Change Color unlocks and reduces the count. Switching releases the old slot.
5. Ready everyone: overlay fades out, usual countdown begins automatically, then gameplay starts.
6. Both clients see matching unique colors. Damage and poison flashes restore each chosen color, not white.
7. Disconnect during selection: reservation frees and totals change. Drop below minimum during countdown: selection returns.
8. Test 3 and 10 clients, smaller/aspect-ratio-changed windows, and multi-peer visibility/input selection.
9. Restart match: fresh selection with no duplicated UI. Test single-player for regressions.

Rebuild all peers together: NetworkMatchManager's network layout and RPCs changed.
Static compilation and palette tests do not validate Fusion weaving, RPC ordering, pointer events, masks or live animation.
