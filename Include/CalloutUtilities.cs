using System;
using Rage;

namespace WhoSaidQuietCallouts
{
    internal static class CalloutUtilities
    {
        public static void WaitForPlayerEnd(string message = "Press ~y~END~s~ to close this callout.")
        {
            Game.DisplayHelp(message);
            Game.LogTrivial("[WSQ][UTIL] Waiting for player to end the callout.");

            bool ended = false;
            while (!ended)
            {
                if (Game.IsKeyDown(System.Windows.Forms.Keys.End))
                {
                    Game.DisplaySubtitle("~g~Callout ended by player.", 3000);
                    ended = true;
                }
                GameFiber.Yield();
            }
        }
    }
}