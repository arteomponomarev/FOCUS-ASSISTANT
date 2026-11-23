using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Focus.Avatar.Protocol;
using UnityEngine;

namespace Focus.Avatar
{
    internal sealed class SessionController : IDisposable
    {
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private Task<string> pending;
        private string verb;
        private float nextAttempt;
        private int failures;
        private float celebrationEnds;
        private float snapshotAt;
        private float idleSince;
        private bool continuousIdle;

        public Guid Session { get; }
        public AvatarMessage Snapshot { get; private set; }
        public string Feedback { get; private set; }
        public bool Busy => pending != null && verb != AvatarProtocol.Snapshot;
        public bool AgentReachable { get; private set; }
        public bool Celebrating(float now) => now < celebrationEnds;
        public double ActiveSeconds(float now) => AvatarPresentation.ActiveSeconds(Snapshot, now - snapshotAt);
        public AvatarMood Mood(float now) => AvatarPresentation.Mood(Snapshot, Celebrating(now),
            continuousIdle ? now - idleSince : 0);

        public SessionController(Guid session)
        {
            Session = session;
            Snapshot = AvatarMessage.Reply(session, "offline");
            Feedback = session == Guid.Empty ? "Pair with a session to connect." : "Connecting to Agent…";
        }

        public void Tick(float now)
        {
            if (pending != null && pending.IsCompleted)
                Receive(now);
            if (Session != Guid.Empty && pending == null && now >= nextAttempt)
                Send(AvatarProtocol.Snapshot);
        }

        public void Command(string action)
        {
            if (pending != null || !AvatarProtocol.CanSend(action, Snapshot.status))
                return;
            Feedback = "Sending to API…";
            Send(action);
        }

        public bool CanCommand(string action) => pending == null && AvatarProtocol.CanSend(action, Snapshot.status);
        private void Send(string action)
        {
            verb = action;
            pending = AvatarConnection.ExchangeAsync(Session, action, stop.Token);
        }

        private void Receive(float now)
        {
            try
            {
                var reply = JsonUtility.FromJson<AvatarMessage>(pending.GetAwaiter().GetResult());
                if (reply == null || !reply.IsValid(Session) ||
                    (verb == AvatarProtocol.Snapshot && reply.result == "accepted") ||
                    (verb != AvatarProtocol.Snapshot && reply.result == "ok"))
                    throw new InvalidDataException();

                AgentReachable = true;
                if (reply.result == "ok")
                {
                    if (AvatarPresentation.ShouldCelebrate(Snapshot.status, reply.status))
                        celebrationEnds = now + (reply.intensity == "Active" ? 4 : 2);
                    var idle = reply.estimate == "Idle";
                    if (idle && !continuousIdle)
                        idleSince = now;
                    continuousIdle = idle;
                    Snapshot = reply;
                    snapshotAt = now;
                    failures = 0;
                    nextAttempt = now + 2;
                }
                else if (verb != AvatarProtocol.Snapshot)
                {
                    Feedback = Message(reply.result);
                    nextAttempt = now;
                }
                else
                {
                    Disconnect(now, Message(reply.result));
                }
                if (reply.result == "ok" && (Feedback == "Connecting to Agent…" || Feedback.StartsWith("Connection")))
                    Feedback = "";
            }
            catch (Exception error) when (error is IOException or TimeoutException or UnauthorizedAccessException or
                ArgumentException or OperationCanceledException or Win32Exception or NotSupportedException)
            {
                AgentReachable = false;
                Disconnect(now, verb == AvatarProtocol.Snapshot
                    ? "Connection unavailable. Retrying…" : "Connection lost. Check the session before retrying.");
            }
            finally
            {
                pending = null;
            }
        }

        private void Disconnect(float now, string message)
        {
            var oldVisibility = Snapshot.visibility;
            Snapshot = AvatarMessage.Reply(Session, "offline");
            // Until preferences can be refreshed, preserve an explicit request to stay hidden.
            Snapshot.visibility = oldVisibility;
            celebrationEnds = 0;
            continuousIdle = false;
            Feedback = message;
            failures = Math.Min(3, failures + 1);
            nextAttempt = now + (1 << failures);
        }

        private static string Message(string result) => result switch
        {
            "accepted" => "Change accepted by API.",
            "unauthorized" => "Connection requires Agent sign-in.",
            "missing" => "Connection cannot access this session.",
            "limited" => "Connection rate limited. Try again shortly.",
            "offline" => "Connection to API unavailable. Retrying…",
            "conflict" => "Session changed. Check the latest status.",
            _ => "The API rejected this action."
        };

        public void Dispose()
        {
            stop.Cancel();
            if (pending != null)
                _ = pending.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            stop.Dispose();
        }
    }
}
