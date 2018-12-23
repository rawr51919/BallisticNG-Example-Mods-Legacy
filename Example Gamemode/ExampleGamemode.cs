using BallisticNG.Gamemodes;
using BallisticNG.Pickups;
using BallisticNG.RaceUI;
using Battlehub.Utils;
using GameData;
using GameData.Constants;
using Settings;
using UnityEngine;
using Source.NgCampaign;
using Source.NgCampaign.Ui;
using BallisticSource.Mods;

namespace BallisticNG.ExampleMods
{
    public class ExampleGamemodeRegister : ModRegister
    {
        public override void OnRegistered()
        {
            /*---Register the gamemode---*/
            GamemodeRegistry.RegisterGamemode("Example Gamemode", new ExampleGamemode(true));
        }
    }

    /* This gamemode is a recreation of the ingame race gamemode but with unlimited shield energy, turbos every lap and no weapons so you can observe how to construct your own gamemodes. Comments are written so you can
    easily see what's going on. No stat code is implemented into this. */
    public class ExampleGamemode : Gamemode
    {
        /* NOTE:
        There currently isn't anything in place to load custom interfaces! If you want to handle your own interfaces right now, you will need to programtically construct them by extending ScriptableMenu and ScriptableHud!
         */

        /*---Interfaces---*/
        private ScriptableMenu _pauseInterface;
        private ScriptableMenu _eliminatedInterface;
        private ScriptableMenu _eventCompleteInterface;

        private ScriptableHud[] _speedAndShieldHud;
        private ScriptableHud[] _notificationHud;
        private ScriptableHud[] _timeHud;
        private ScriptableHud[] _positionHud;
        private ScriptableHud[] _lapHud;
        private ScriptableHud[] _weaponHud;
        private ScriptableHud[] _nowPlayingHud;

        /* This empty constructor is needed for when the gamemode is copied into the race manager. */
        public ExampleGamemode()
        {

        }

        /* This constructor is a wrapper for the default gamemode constructor, this is needed also. */
        public ExampleGamemode(bool manualConfiguration) : base(manualConfiguration)
        {

        }

        /* This is where we configure the configuration settings for the race - for this example gamemode I am just disabling the afterburner. */
        public override void OnSetupConfig()
        {
            Configuration.AfterburnerEnabled = false;
        }

        /* This is called when a track is first loaded, this is where you should do any initial configuration for your gamemode. */
        public override void OnAwake()
        {
            // this calls the original function, whenever you see these in this example gamemode you need to include them in your own too!
            base.OnAwake();

            // allow race manage to determine the number of laps
            RaceManager.Instance.AutoLapSetup = true;

            // number of racers in the race
            Race.RacerCount = 1 + Race.AiCount;
        }

        /* This is called when everything has been setup, you can use this to do any post-ship spawning configurations. */
        public override void OnStart()
        {
            // this base call is very important and you need this, the original function does a bunch of work to prepare this gamemode
            base.OnStart();

            // load the time for track into the players ship
            if (Ships.LoadedShips.Count > 0) LoadTime(Ships.LoadedShips[0]);
        }

        /* This is called every frame. */
        public override void OnUpdate() 
        {
            base.OnUpdate();

            // calculates the position of each racer
            RaceManager.Instance.RacePositionManager.CalculateRacePositions();
        }

        /* This is called every fixed update. */
        public override void OnFixedUpdate()
        {
            /* Calling this updates the timers for the current race on each ship. The first parameter controls whether the total time will be updated and the
            second parameter controls whether the current lap time will be updated. */
            UpdateRaceTimers(true, true);

            // iterate through each ship and reset the shield integrity to 100
            for (int i = 0; i < Ships.LoadedShips.Count; ++i) Ships.LoadedShips[i].ShieldIntegrity = 100.0f;
        }

        /* This is called when the race manager is destroyed. */
        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        /* This is called every time a ship passes the start line, r is the reference to the ship that has passed. */
        public override void OnShipTriggerStartLine(ShipRefs r)
        {
            /* If this lap has been validated or the ship hasn't done any laps yet, then we want to do some stuff. */
            if (r.LapValidated || r.CurrentLap == 0)
            {
                // invalidate the lap again
                r.LapValidated = false;

                /* If the ship has finished a lap and is not on the last lap then we want to store and display some information. */
                if (r.CurrentLap > 0 && r.CurrentLap <= Race.MaxLaps)
                {
                    if (r.IsPlayer)
                    {
                        /* Update best time */
                        if ((r.CurrentLapTime < r.BestLapTime || !r.HasBestLapTime) && !r.LoadedBestLapTime)
                        {
                            r.BestLapTime = r.CurrentLapTime;
                            r.HasBestLapTime = true;
                        }

                        /* Perfect lap notification */
                        if (r.IsPerfectLap)
                        {
                            // this triggers an onscreen message to appear. You can provide a color or you can write it using richtext in the string.
                            BallisticEvents.Ui.CallOnTriggerMessage("PERFECT LAP", r, ScriptableHud.BnGAccent);

                            // this plays a voice, you can feed this any sound you want (if you load your own you can also use that).
                            AudioHelpers.PlayVoice(AudioHelpers.Voice_PerfectLap);
                        }

                        /* Interface sounds */
                        if (r.CurrentLap == Race.MaxLaps - 1)
                        {
                            BallisticEvents.Ui.CallOnTriggerMessage("FINAL LAP", r, ScriptableHud.BnGAccent);
                            AudioHelpers.PlayVoice(AudioHelpers.Voice_FinalLap);
                        }
                        AudioHelpers.PlayOneShot(AudioHelpers.UI_Checkpoint, AudioHelpers.E_AUDIOCHANNEL.INTERFACE, 1.0f, 1.0f);
                    }

                    // set values for current lap
                    r.LapTimes[r.CurrentLap - 1] = r.CurrentLapTime;
                    r.PerfectLaps[r.CurrentLap - 1] = r.IsPerfectLap;
                }
                
                /* Tasks for when the ship has completed the race */
                if (r.CurrentLap >= Race.MaxLaps && !r.FinishedEvent && !r.Eliminated)
                {
                    r.FinishedEvent = true;

                    // calling this does some needed config to mark the ship as having finished
                    RaceHelpers.FinishRace(r);

                    // if this is a player ship then set the ship as an AI ship and then save the time
                    if (r.IsPlayer)
                    {
                        // destroy the ship camera and replace it with the finished camera
                        Object.Destroy(r.ShipCamera.GetComponent<ShipCamera>());
                        ShipFCam finishCam = r.ShipCamera.gameObject.AddComponent<ShipFCam>();
                        finishCam.r = r;

                        r.IsAi = true;
                        SaveTime(r);
                    }
                }

                /* Reset timers and states */
                // this resets the current laps time to zero
                r.CurrentLapTime = 0.0f;
                r.IsPerfectLap = true;
                ++r.CurrentLap;
                r.PassedValidationGate = false;

                // this clears the hit weapon pads so ships can use them again
                r.ClearHitPads();
                BallisticEvents.Race.CallOnShipLapUpdate(r);

                // this calculates the time between this ship and another (depending on the ships position)
                if (r.IsPlayer) CalculateAndDisplayRelativeTime(r);

                // give the ship a turbo
                PickupRegistry.GivePickupToShip(r, PickupRegistry.FindPickupByName("turbo"));
            }
        }

        /* This is called every time a ship passes the mid line. r is the reference to the ship that has passed. */
        public override void OnShipTriggerMidLine(ShipRefs r)
        {
            // validate the lap so it can be updated next time the player triggers the start line
            r.LapValidated = true;
            r.MiddleSection = r.CurrentSection;

            if (r.IsPlayer && !r.PassedValidationGate)
            {
                AudioHelpers.PlayOneShot(AudioHelpers.UI_Checkpoint, AudioHelpers.E_AUDIOCHANNEL.INTERFACE, 1.0f, 1.0f);
                CalculateAndDisplayRelativeTime(r);

                r.PassedValidationGate = true;
            }
        }

        /* This is called every time a ship passes the mid line reset gate, setup just behind the mid line gate. */
        public override void OnShipTriggerMidLineReset(ShipRefs r)
        {
            // invalidate this lap
            r.LapValidated = false;
        }

        /* This is called every time a ship is spawned. */
        public override void OnShipSpawn(ShipRefs r)
        {
            base.OnShipSpawn(r);
        }

        /* This is called everytime a ship reaches 0% shield energy. What you do here is up to you, the base behaviour calls EliminateShip(r). */
        public override void OnShipExploded(ShipRefs r)
        {
            base.OnShipExploded(r);

            // if this is the player who has died then open the eliminated interface after a short delay.
            if (r.IsPlayer) _eliminatedInterface.OpenDelayed(1.0f);
        }

        /* This is called everytime a ship finishes the race. */
        public override void OnShipFinished(ShipRefs r)
        {
            base.OnShipFinished(r);
        }

        /* This is called everytime a ship starts a new lap. */
        public override void OnShipLapUpdate(ShipRefs r)
        {
            base.OnShipLapUpdate(r);
        }

        /* This is called when every player has finished the race. */
        public override void OnEventComplete()
        {
            base.OnEventComplete();

            // open the event complete interface
            if (_eventCompleteInterface) _eventCompleteInterface.Open();
        }

        /* This is called whenever the game is paused. */
        public override void OnGamePaused()
        {
            base.OnGamePaused();
        }

        /* This is called whenever the game is unpaused. */
        public override void OnGameUnpaused()
        {
            base.OnGameUnpaused();
        }

        /* This is called when the event has finished and the player is exiting. */
        public override void OnEventExit()
        {
            base.OnEventExit();
        }

        /* Call this to save the time for this track. Default behaviour saves the ships total time to disk. */
        public override void SaveTime(ShipRefs r)
        {
            base.SaveTime(r);
        }

        /* Call this to load the time for this track. Default behaviour loads the file that is saved to disk by SaveTime(r). */
        public override void LoadTime(ShipRefs r)
        {
            base.LoadTime(r);
        }

        /* This is called for interfaces to be loaded. */
        public override void LoadInterfaces()
        {
            /*---Menus---*/
            _pauseInterface = InterfaceLoader.LoadMenu(InterfaceLoader.Menus.EventPause, false);
            _eliminatedInterface = InterfaceLoader.LoadMenu(InterfaceLoader.Menus.Eliminated, false);
            _eventCompleteInterface = InterfaceLoader.LoadMenu(NgCampaign.Enabled ? InterfaceLoader.Menus.EventCompleteStandardCampaign : InterfaceLoader.Menus.EventCompleteStandard, false);

            /*---HUDs---*/
            _speedAndShieldHud = CreateNewHuds(InterfaceLoader.Huds.SpeedAndShield);
            _notificationHud = CreateNewHuds(InterfaceLoader.Huds.NotificationBuffer);
            _timeHud = CreateNewHuds(InterfaceLoader.Huds.TimeStandard);
            _positionHud = CreateNewHuds(InterfaceLoader.Huds.Position);
            _lapHud = CreateNewHuds(InterfaceLoader.Huds.Lap);
            _weaponHud = CreateNewHuds(InterfaceLoader.Huds.Weapon);
            _nowPlayingHud = CreateNewHuds(InterfaceLoader.Huds.NowPlaying);
        }

        /* This is called for interfaces to be destroyed. */
        public override void DestroyInterfaces()
        {
            /*---Menus---*/
            if (_pauseInterface) Object.Destroy(_pauseInterface.gameObject);
            if (_eliminatedInterface) Object.Destroy(_eliminatedInterface.gameObject);
            if (_eventCompleteInterface) Object.Destroy(_eventCompleteInterface.gameObject);

            /*---HUDs---*/
            if (_speedAndShieldHud != null) DestroyHuds(_speedAndShieldHud);
            if (_notificationHud != null) DestroyHuds(_notificationHud);
            if (_timeHud != null) DestroyHuds(_timeHud);
            if (_positionHud != null) DestroyHuds(_positionHud);
            if (_lapHud != null) DestroyHuds(_lapHud);
            if (_weaponHud != null) DestroyHuds(_weaponHud);
            if (_nowPlayingHud != null) DestroyHuds(_nowPlayingHud);
        }
    }
}
