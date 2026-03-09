using System;
using Rage;
using LSPD_First_Response.Mod.Callouts;

namespace WhoSaidQuietCallouts
{
    /// <summary>
    /// WSQCalloutBase.cs
    /// Version: 0.9.0 Base Framework (shared foundation for WSQ callouts)
    /// Provides unified logging, safe cleanup, and player-controlled callout ending.
    /// </summary>
    public abstract class WSQCalloutBase : Callout
    {
        /// <summary>
        /// Waits for the player to manually end the callout using END key.
        /// </summary>
        protected void PlayerControlledEnd(string prompt = "Press ~y~END~s~ when you're ready to end this callout.")
        {
            Game.DisplayHelp(prompt);
            Game.LogTrivial("[WSQ][Base] Waiting for player to end the callout...");

            GameFiber.StartNew(delegate
            {
                bool waiting = true;
                while (waiting)
                {
                    if (Game.IsKeyDown(System.Windows.Forms.Keys.End))
                    {
                        Game.DisplaySubtitle("~g~Callout ended by player.", 3000);
                        waiting = false;
                        break;
                    }
                    GameFiber.Yield();
                }
                End();
            });
        }

        /// <summary>
        /// Shared cleanup routine for derived callouts.
        /// </summary>
        protected void CleanupEntities(params Entity[] entities)
        {
            foreach (var ent in entities)
            {
                try
                {
                    if (ent && ent.Exists()) ent.Dismiss();
                }
                catch (Exception ex)
                {
                    Game.LogTrivial($"[WSQ][Base] Cleanup exception: {ex.Message}");
                }
            }
        }

        public override void End()
        {
            Game.LogTrivial("[WSQ][Base] Callout ended and cleanup complete.");
            Game.DisplayNotification("CHAR_POLICE", "CHAR_POLICE",
                "Dispatch", "Callout Completed", "Scene cleared and all entities dismissed.");
            base.End();
        }
    }
}