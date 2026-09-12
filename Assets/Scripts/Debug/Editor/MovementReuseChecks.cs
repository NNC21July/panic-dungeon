using System;
using System.Reflection;
using Fusion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class MovementReuseChecks
{
    [MenuItem("Tools/Panic Dungeon/Check Shared Movement")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before running the movement checks.");

        CheckPlayer("Assets/Prefabs/Player.prefab", false);
        CheckPlayer("Assets/Prefabs/NetworkPlayer.prefab", true);
        Debug.Log("Movement reuse checks passed: prefab wiring, movement, braking, dash, cooldown, stop, and reset.");
    }

    private static void CheckPlayer(string path, bool multiplayer)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Require(prefab != null, "Missing player prefab: " + path);

        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            PlayerDash dash = player.GetComponent<PlayerDash>();
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            Health health = player.GetComponent<Health>();

            Require(movement != null && dash != null && rb != null, "Missing shared movement components: " + path);
            SerializedFieldValidator.Validate(movement);
            SerializedFieldValidator.Validate(dash);

            if (multiplayer)
            {
                Require(health == null, "Multiplayer must not gain single-player Health.");
                Require(!movement.enabled && !dash.enabled, "Network movement must start disabled until ownership is known.");
                Require(player.GetComponent<NetworkPlayerMovementSync>() != null, "Missing position synchronization.");
                Require(player.GetComponent<NetworkTransform>() == null, "The old position writer is still attached.");
                Require(player.GetComponent("NetworkPlayerSnapshotPreview") == null, "The preview is still attached.");
                PlayerInput input = player.GetComponent<PlayerInput>();
                Require(input == null || !input.enabled, "Remote players must not read single-player input.");
            }

            // These are edit-mode checks: initialize only the components under test.
            if (health != null)
                Invoke(health, "Awake");
            Invoke(movement, "Awake");
            Invoke(dash, "Awake");
            movement.ResetMovement();
            dash.ResetDash();

            PlayerTuning tuning = (PlayerTuning)Get(movement, "playerTuning");
            movement.SetMoveInput(Vector2.left);
            Require(movement.GetLastMoveDirection() == Vector2.left, "Movement direction was not remembered.");
            Invoke(movement, "FixedUpdate");
            Require(rb.linearVelocity.x < 0f, "Walking did not apply velocity.");
            float walkingSpeed = rb.linearVelocity.magnitude;
            movement.SetMoveInput(Vector2.zero);
            Invoke(movement, "FixedUpdate");
            Require(rb.linearVelocity.magnitude < walkingSpeed, "Walking did not brake.");

            Require(dash.TryDash(), "An available dash was rejected.");
            Require(!dash.TryDash(), "A second dash started while already dashing.");
            Invoke(dash, "FixedUpdate");
            Vector2 expectedVelocity = PlayerMovementMotion.CalculateDashVelocity(
                Vector2.left, tuning.DashSpeed, 0f, tuning.DashSpeedCurve);
            Require(Vector2.Distance(rb.linearVelocity, expectedVelocity) < 0.0001f, "Dash motion changed.");

            Invoke(movement, "FixedUpdate");
            Require(Vector2.Distance(rb.linearVelocity, expectedVelocity) < 0.0001f, "Walking overwrote the dash.");
            Require((float)Get(dash, "cooldownTimer") == 0f, "Cooldown started before the dash ended.");

            Set(dash, "dashTimer", -1f);
            Invoke(dash, "Update");
            Require(!dash.IsDashing(), "Dash did not end.");
            Require(!dash.TryDash(), "Cooldown did not block another dash.");

            dash.ResetDash();
            Require(dash.TryDash(), "Reset did not restore dash availability.");
            dash.CancelDash();
            Require(!dash.IsDashing() && !dash.TryDash(), "Correction cancellation bypassed dash cooldown.");

            movement.StopMovement();
            dash.StopDash();
            movement.SetMoveInput(Vector2.right);
            Require(!movement.enabled && !dash.enabled && rb.linearVelocity == Vector2.zero, "Stop did not disable movement.");
            Require(!dash.TryDash(), "Disabled dash accepted input.");

            movement.ResetMovement();
            dash.ResetDash();
            if (health != null)
            {
                health.TakeDamage(new DamageInfo(health.MaxHealth, player, DamageType.Melee));
                Require(!movement.enabled && !dash.enabled, "Single-player death did not stop movement.");
                health.ResetHealth();
                movement.ResetMovement();
                dash.ResetDash();
            }
            Require(dash.TryDash(), "Movement did not recover after reset.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void Invoke(object target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }

    private static object Get(object target, string field)
    {
        return target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
