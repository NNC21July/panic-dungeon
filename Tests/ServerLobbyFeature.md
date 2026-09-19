# Server-only lobby checkpoint

## Scope

This is the local-development lobby flow, not a cloud server deployment or matchmaking service.
No movement, trap, damage, or in-match restart logic was changed.

## What happens

1. In manual startup, Create Room starts a `GameMode.Server` runner plus one separate client runner for
   the creator, using the existing FusionBootstrap. The server has no player. It runs on the creator's computer
   for now; closing that application also stops its server. This requires Peer Mode = Multiple.
2. Join connects a client to an existing room code using the existing bootstrap client startup.
3. A scene NetworkObject in MultiplayerSandbox contains NetworkLobbyManager. The server replicates the leader,
   connected count, spawned-player count, loading flag, and scene-load error state.
4. The first available client becomes leader (lowest PlayerRef if several are present when election runs).
   The server keeps that leader until they leave, then chooses a remaining client. An empty lobby has no leader.
   This role is permission to REQUEST Start, not state authority, server ownership, or host migration.
5. Only the leader sees Start Game. It is enabled when at least two players have server-registered player objects
   and all connected players have such objects. "Ready" here means spawned, not a separate player-ready checkbox.
6. Clicking Start sends a reliable RPC to the lobby's state authority. The server checks the actual `RpcInfo.Source`,
   current leader membership, current player counts, duplicate-start guard, and configured game scene.
   The client cannot choose a scene path or force loading by enabling its UI button.
7. The server closes room admission and loads MultiplayerGame for everyone. Room admission stays closed during
   the match; late joining/reconnecting mid-match is not provided by this checkpoint. A load failure reopens the
   session when the lobby object still exists. If the scene has already unloaded, recovery requires a session restart.
8. The existing game scene spawner and match manager handle player spawning/countdown as before.

## Files

- NetworkLauncher.cs: existing UI, Create/Join, leader-only Start request, status text and initial local-client selection.
- NetworkLobbyManager.cs: replicated lobby state and server-only authorization/loading.
- MultiplayerSandbox.unity: lobby object and UI references are already wired. Create Room replaces the Host label;
  its text auto-sizes within the existing button layout. The existing automatic server + two clients preset is preserved.

When a local client first connects, the first local client gets the visible camera/input; the non-playing server
and extra clients are hidden/input-disabled. This only selects the initial view. Runner Controls can still switch
view/input afterward, and hidden peers keep simulating.

## Test A: existing multi-peer preset

1. Open MultiplayerSandbox and enter Play mode. The saved preset starts Server + Client A + Client B.
2. Client A should initially be visible and controllable, and normally becomes leader.
3. Verify two player objects (not three players); the server must have no local player.
4. A's UI should identify the lobby leader and show Start Game once both players have spawned.
5. Shift-click B's eye and input icons in Network Runner Controls. B must NOT have a Start Game button.
6. Switch back to A and click Start Game once. Both clients should load MultiplayerGame and see the existing countdown.
7. Stop/restart Play mode for another test. The in-game Restart button is intentionally still server-restricted.

## Test B: creation and joining

1. Set FusionBootstrap Start Mode to Manual before Play. Enter a room code and click Create Room.
2. Verify Server + Client A, not a Host runner. A is leader, with Start disabled while alone.
3. Use the persistent bootstrap's Add Additional Client button to add B in the editor, or join from a matching build.
4. The leader's Start button should enable after both player objects are registered.
5. Test a blank room code (must refuse), a nonexistent room code, and a failed connection. Fusion logs connection
   failures; if startup fails, stop/restart Play mode or restart the application. Full reconnect/retry UI is deferred.
6. Normal standalone player builds force manual startup even if the scene was saved with the editor automatic test
   preset. They do not automatically create a test server and two clients. There is no headless deployment entry point yet.

## Test C: leader departure and rejected starts

- Disconnect the leader's client while leaving the server runner alive. The remaining client should become leader.
  With only one player left, Start must remain disabled. Add/join another client, then request Start from the new leader.
- Sending a Start request from a non-leader, from the server's PlayerRef.None, or after loading began must not start
  another load. A client button alone is not the security check.
- If another player leaves before the server processes Start, the server recounts players and refuses an insufficient lobby.
- Use separate processes for the final pass. Closing the application that also runs the local server is NOT the same
  as disconnecting just its client; it ends the whole session.
- Current sandbox has two spawn points. More clients require matching lobby spawn capacity; this step does not add it.

## Verification limits

The complete game C# compile and static scene-wiring checks were run without starting Unity.
That does not verify Fusion weaving, live RPC delivery, UI layout rendering, or scene transitions in Play mode.
Run the checks above before treating this as a tested lobby checkpoint.

Fusion RPC sender rules: https://doc.photonengine.com/fusion/v2/manual/data-transfer/rpcs
