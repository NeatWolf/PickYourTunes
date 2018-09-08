using GTA;
using GTA.Native;
using NAudio.Wave;
using PickYourTunes.Properties;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

namespace PickYourTunes
{
    public class PickYourTunes : Script
    {
        /// <summary>
        /// The mod configuration.
        /// </summary>
        ScriptSettings Config = ScriptSettings.Load("scripts\\PickYourTunes.ini");
        /// <summary>
        /// The location where our sounds are loaded.
        /// Usually <GTA V>\scripts\PickYourTunes
        /// </summary>
        string SongLocation = new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase), "PickYourTunes")).LocalPath;
        /// <summary>
        /// Our instance of WaveOutEvent that plays our custom files.
        /// </summary>
        WaveOutEvent OutputDevice = new WaveOutEvent();
        /// <summary>
        /// The file that is currently playing.
        /// </summary>
        AudioFileReader CurrentFile;
        /// <summary>
        /// The vehicle that the player was using previously.
        /// </summary>
        int PreviousVehicle;

        public PickYourTunes()
        {
            // Patch our locale so we don't have the "coma vs dot" problem
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            // Set our events for the script and player
            Tick += OnTick;
            Tick += Cheats.OnCheat;
            OutputDevice.PlaybackStopped += OnStop;
            
            // Set the volume to the configuration value
            OutputDevice.Volume = Config.GetValue("General", "Volume", 0.2f);

            // Check that the directory with our scripts exists
            // If not, create it
            if (!Directory.Exists(SongLocation))
            {
                Directory.CreateDirectory(SongLocation);
            }
        }

        private void OnTick(object Sender, EventArgs Args)
        {
            // Just a hack recommended by "Slick" on the 5mods server to keep the radio disabled
            // "I made my own by setting the radio per tick, not the best way but hey it works"
            if (OutputDevice.PlaybackState == PlaybackState.Playing && Game.Player.Character.CurrentVehicle != null)
            {
                Function.Call(Hash.SET_VEH_RADIO_STATION, Game.Player.Character.CurrentVehicle, "OFF");
            }

            // If the game is paused OR the engine is not running AND the audio is not stopped
            // Pause it, because is running and we are not in a vehicle
            if ((Game.IsPaused || !Checks.IsEngineRunning() || Checks.HashSameAsCurrent(PreviousVehicle)) && OutputDevice.PlaybackState != PlaybackState.Stopped)
            {
                OutputDevice.Pause();
            }
            // If the statement above didn't worked (not paused but on a running vehicle)
            // Resume the playback
            else if (OutputDevice.PlaybackState == PlaybackState.Paused)
            {
                OutputDevice.Play();
            }
            // If none of the above worked out, there is nothing playing nor loaded
            // Load the configuration value and check what is going on
            else if (OutputDevice.PlaybackState != PlaybackState.Playing)
            {
                // Store the vehicle that the player is getting into
                Vehicle CurrentVehicle = Game.Player.Character.GetVehicleIsTryingToEnter();
                // Store the hash that we have
                PreviousVehicle = CurrentVehicle.Model.GetHashCode();
                // Store our radio and song for the vehicle
                int VehicleRadio = Config.GetValue("Radios", CurrentVehicle.Model.GetHashCode().ToString(), 256);
                string VehicleSong = Config.GetValue("Audio", CurrentVehicle.Model.GetHashCode().ToString(), string.Empty);
                // Store our radio and song for all of them
                int GenericRadio = Config.GetValue("General", "Radio", 256);
                string GenericSong = Config.GetValue("General", "Audio", "null");

                // Replace our vehicle values for the default ones if the user wants to
                if (GenericSong != "null")
                {
                    VehicleSong = GenericSong;
                }
                else if (GenericRadio != 256)
                {
                    VehicleRadio = GenericRadio;
                }

                // If there is a song requested and the music is stopped, play it
                if (VehicleSong != string.Empty && OutputDevice.PlaybackState == PlaybackState.Stopped)
                {
                    if (!File.Exists(Path.Combine(SongLocation, VehicleSong)))
                    {
                        UI.Notify(string.Format(Resources.FileWarning, VehicleSong));
                        return;
                    }

                    // Store our current file
                    CurrentFile = new AudioFileReader(Path.Combine(SongLocation, VehicleSong));
                    // Initialize it
                    OutputDevice.Init(CurrentFile);
                    // And play it
                    OutputDevice.Play();
                }
                // Else if our default value is not 256 (aka invalid or not added), change the radio
                else if (VehicleRadio != 256)
                {
                    Tools.SetRadioInVehicle(VehicleRadio, CurrentVehicle);
                }
            }
        }

        private void OnStop(object Sender, StoppedEventArgs Args)
        {
            // if the current file exists
            if (CurrentFile != null)
            {
                // Dispose it
                CurrentFile.Dispose();
                // And remove it
                CurrentFile = null;
            }
        }
    }
}
