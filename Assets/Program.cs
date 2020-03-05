/*  
    This file is part of ContrAlto.

    ContrAlto is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    ContrAlto is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with ContrAlto.  If not, see <http://www.gnu.org/licenses/>.
*/

using Contralto.Scripting;
using Contralto.SdlUI;
using System;
using UnityEngine;
using Contralto;
//using System.Windows.Forms;

namespace Contralto
{
    /*public static class StartupOptions
    {
        public static string ConfigurationFile;
        //public static string ConfigurationFile;

        public static string ScriptFile;

        public static string RomPath;
    }*/

    public class Program : MonoBehaviour
    {
        public string ConfigurationFile;

        public string ScriptFile;

        public string RomPath;
        public string[] args = { "-config", "Contralto.cfg", "-script", "script.scp" };
        public string Drive0Image;
        public string Drive1Image;
        public int Station;
        public int inputKey=1;

        public AltoSystem _system;

        void Start()
        {
            
            UMain(args);   
        }
        //[STAThread]
        void UMain(string[] args)
        {
            //
            // Check for command-line arguments.
            //            
            /*if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i++].ToLowerInvariant())
                    {
                        case "-config":
                            if (i < args.Length)
                            {
                                StartupOptions.ConfigurationFile = args[i];
                            }
                            else
                            {
                                PrintUsage();
                                return;
                            }
                            break;

                        case "-script":
                            if (i < args.Length)
                            {
                                StartupOptions.ScriptFile = args[i];
                            }
                            else
                            {
                                PrintUsage();
                                return;
                            }
                            break;

                        case "-rompath":
                            if (i < args.Length)
                            {
                                StartupOptions.RomPath = args[i];
                            }
                            else
                            {
                                PrintUsage();
                                return;
                            }
                            break;

                        default:
                            PrintUsage();
                            return;
                    }
                }
            }*/
            
            //PrintHerald();

            _system = new AltoSystem((byte)Station);
            //_system1 = new AltoSystem();
            

            // Load disks specified by configuration
            if (!String.IsNullOrEmpty(Drive0Image))
            {
                try
                {
                    _system.LoadDiabloDrive(0, Drive0Image, false);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Could not load image '{0}' for Diablo drive 0.  Error '{1}'.", Configuration.Drive0Image, e.Message);
                    _system.UnloadDiabloDrive(0);
                }
            }

            if (!String.IsNullOrEmpty(Drive1Image))
            {
                try
                {
                    _system.LoadDiabloDrive(1, Drive1Image, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not load image '{0}' for Diablo drive 1.  Error '{1}'.", Configuration.Drive1Image, e.Message);
                    _system.UnloadDiabloDrive(1);
                }
            }

            /*if (!String.IsNullOrEmpty(Configuration.Drive0Image))
            {
                try
                {
                    _system1.LoadDiabloDrive(0, Configuration.Drive0Image, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not load image '{0}' for Diablo drive 0.  Error '{1}'.", Configuration.Drive0Image, e.Message);
                    _system1.UnloadDiabloDrive(0);
                }
            }

            if (!String.IsNullOrEmpty(Configuration.Drive1Image))
            {
                try
                {
                    _system1.LoadDiabloDrive(1, Configuration.Drive1Image, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not load image '{0}' for Diablo drive 1.  Error '{1}'.", Configuration.Drive1Image, e.Message);
                    _system1.UnloadDiabloDrive(1);
                }
            }*/



            //
            // Attach handlers so that we can properly flush state if we're terminated.
            //
            //AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            {
                mainWindow = new SdlAltoWindow();
                {
                    // Invoke the command-line console
                    //console = new SdlConsole(_system);
                    //console.Run(mainWindow);

                    // Start the SDL display running.
                    mainWindow.AttachSystem(_system);
                }
 
                _controller = new ExecutionController(_system);
                _controller.StartExecution(AlternateBootType.None);

            }
        }

        public bool input=false;
        public Texture2D Altotex;
        SdlAltoWindow mainWindow;
        ExecutionController _controller;

        public float nexttime;
        public float delta = 0.5f;

        public void Update()
        {
            if (nexttime < Time.time)
            {
                if (Input.GetKeyDown(KeyCode.F5 + _system.address-1)) input = !input;
                if (Input.GetKeyDown(KeyCode.F10)) input = true;
                if (Input.GetKeyDown(KeyCode.F9)) input = false;
                mainWindow.Run(Altotex,input);
                nexttime = Time.time + delta;
            }
        }

        /*public void OnApplicationQuit();
        {
            Console.WriteLine("Exiting...");
                        
            _system.Shutdown(false) /* don't commit disks;

            //
            // Commit current configuration to disk
            //
            //Configuration.WriteConfiguration();
        }*/

        /*private static void PrintHerald()
        {
            Console.WriteLine("ContrAlto v{0} (c) 2015-2018 Living Computers: Museum+Labs.", typeof(Program).Assembly.GetName().Version);
            Console.WriteLine("Bug reports to joshd@livingcomputers.org");
            Console.WriteLine();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: ContrAlto [-config <configurationFile>] [-script <scriptFile>]");
        }*/

        
        //private static AltoSystem _system1;
    }
}
