# Predicted sword contact feedback

The owning client checks its rendered sword against zombies in LateUpdate, after synchronizing collider transforms.
Visible overlap previews DamageFlash only. It does not change health, emit OnDamaged, kill a zombie or send a damage RPC.
The server still uses the existing sword overlap and per-swing target set to authorize damage.

Each accepted swing stores its Fusion start tick. Confirmations carry that tick and the attacker's NetworkId.
Each target remembers up to 128 attacker/tick pairs: preview-first suppresses the later duplicate flash;
confirmation-first prevents a subsequent preview from flashing again. Other attackers and other swings remain independent.
Confirmed OnDamaged events still run, allowing confirmed damage numbers to be added separately later.
Rejected previews simply finish; bounded history and spawn/despawn cleanup prevent unlimited accumulation.

## Manual checks (server and two clients, including latency)

1. Swing into a zombie: the attacker sees red on visible contact, before confirmation. No second flash on confirmation.
2. The observer sees the confirmed red flash. Both clients eventually agree on zombie health/death.
3. Keep the sword overlapping for several frames: one preview for that swing. A new swing can flash again.
4. Both players attack the same zombie: neither player's confirmation suppresses the other player's hit.
5. Miss the zombie: no preview. If a locally overlapping swing is rejected, no health loss/death is predicted.
6. Check arrow/poison feedback, player-versus-player melee, enemy death and restart for regressions.
7. Under packet loss or prediction correction, check for duplicate flashes: deduplication requires the predicted and
   server-accepted swing to have the same start tick. This does not add melee lag compensation or make hit rejection impossible.

Rebuild both clients together: this change adds networked swing state and changes the damage-feedback RPC signature.
Static compilation/regression checks do not exercise Fusion weaving, prediction rollback, RPC delivery or live rendered overlap.
