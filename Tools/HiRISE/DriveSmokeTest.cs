using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Transient play-mode test using the existing controller and a virtual keyboard.</summary>
public static class DriveSmokeTest
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        RoverController rover = UnityEngine.Object.FindAnyObjectByType<RoverController>();
        Rigidbody body = rover.GetComponent<Rigidbody>();
        Keyboard previous = Keyboard.current;
        Keyboard keyboard = null;
        var background = InputSystem.settings.backgroundBehavior;
        Vector3 start = rover.transform.position;
        int firstFrame = Time.frameCount;
        float startedAt = Time.time, maximumTilt = 0f, maximumSpeed = 0f;
        int groundedObservations = 0, observations = 0;
        int inputObservations = 0;
        float maximumMotorTorque = 0;
        Action holdForward = () =>
        {
            keyboard.MakeCurrent();
            InputState.Change(keyboard, new KeyboardState(Key.W));
        };
        try
        {
            // The Editor can tick while unfocused yet suppress virtual keyboard
            // events. Override this for the test only; never change game controls.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>("HiRISE test keyboard");
            InputSystem.onAfterUpdate += holdForward;
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            DateTime deadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < deadline && Time.time - startedAt < 12f)
            {
                await Task.Delay(100);
                observations++;
                if (Keyboard.current == keyboard && keyboard.wKey.isPressed) inputObservations++;
                foreach (var axle in rover.axles)
                    maximumMotorTorque = Mathf.Max(maximumMotorTorque, Mathf.Abs(axle.LWColl.motorTorque),
                        Mathf.Abs(axle.RWColl.motorTorque));
                float tilt = Vector3.Angle(rover.transform.up, Vector3.up);
                maximumTilt = Mathf.Max(maximumTilt, tilt);
                maximumSpeed = Mathf.Max(maximumSpeed, body.linearVelocity.magnitude);
                foreach (var axle in rover.axles)
                    if (axle.LWColl.isGrounded || axle.RWColl.isGrounded)
                    {
                        groundedObservations++;
                        break;
                    }
                if (tilt > 65f) break;
                if (Vector2.Distance(new Vector2(start.x, start.z),
                    new Vector2(rover.transform.position.x, rover.transform.position.z)) >= 100f) break;
            }
        }
        finally
        {
            InputSystem.onAfterUpdate -= holdForward;
            if (keyboard != null)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.RemoveDevice(keyboard);
            }
            InputSystem.settings.backgroundBehavior = background;
            if (previous != null) previous.MakeCurrent();
        }
        float distance = Vector2.Distance(new Vector2(start.x, start.z),
            new Vector2(rover.transform.position.x, rover.transform.position.z));
        object result = new
        {
            framesAdvanced = Time.frameCount - firstFrame, gameSeconds = Time.time - startedAt,
            distanceMeters = distance, maximumTiltDegrees = maximumTilt,
            maximumSpeedMetersPerSecond = maximumSpeed,
            groundedObservations, observations,
            inputObservations, maximumMotorTorque,
            passed = Time.frameCount > firstFrame && distance >= 25f && maximumTilt < 65f &&
                groundedObservations > observations / 2,
            start = start.ToString(), end = rover.transform.position.ToString()
        };
        var generator = UnityEngine.Object.FindAnyObjectByType<MarsTerrainGenerator>();
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(generator.manifestPath), "drive-test.json"),
            Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented) + "\n");
        return result;
    }
}
