
using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Text;

using CTRE.Phoenix;
using CTRE.Phoenix.Controller;
using CTRE.Phoenix.MotorControl;
using CTRE.Phoenix.MotorControl.CAN;

namespace HERO_XInput_Gampad_Example
{
    public class Program
    {
        GameController _gamepad = new GameController(UsbHostDevice.GetInstance(0)); //creating a controller object
        static CTRE.Phoenix.PneumaticControlModule _pcm = new CTRE.Phoenix.PneumaticControlModule(0); //creating a PCM object
        SafeOutputPort digitalOut1 = new SafeOutputPort(CTRE.HERO.IO.Port1.Pin4, false);  //PIN 4 > IN2
        SafeOutputPort digitalOut2 = new SafeOutputPort(CTRE.HERO.IO.Port1.Pin5, false);   //PIN 5 > IN1
        InputPort inputDead = new InputPort(CTRE.HERO.IO.Port6.Pin4, false, Port.ResistorMode.PullDown);
        InputPort inputPressure = new InputPort(CTRE.HERO.IO.Port6.Pin5, false, Port.ResistorMode.Disabled);
        TalonSRX tal1 = new TalonSRX(1); //first Talon, ID = 1
        TalonSRX tal2 = new TalonSRX(2);//second Talon, ID = 2
        long kTimeoutPeriod;
        long solenoidPeriod;

        public void RunForever()
        {
            _pcm.StopCompressor();
            Boolean SolenoidTimer = false;
            tal1.ConfigFactoryDefault();
            tal2.ConfigFactoryDefault();

            int i = 0;
            while (true)
            {

                if (_gamepad.GetConnectionStatus() == UsbDeviceConnection.Connected)
                {

                    CTRE.Phoenix.Watchdog.Feed();
                }
                else
                {
                    Debug.Print("Not connected: " + i);
                }
                i++;


                //Linear Actuator
                Boolean Stopmovement = _gamepad.GetButton(1); //X-Button
                Boolean Extend = _gamepad.GetButton(2);//A-Button
                Boolean Retract = _gamepad.GetButton(3); //B-Button
                if (Extend)
                {
                    digitalOut1.Write(false);
                    digitalOut2.Write(true);
                }
                if (Retract)
                {
                    digitalOut1.Write(true);
                    digitalOut2.Write(false);
                }
                if (Stopmovement)
                {
                    digitalOut1.Write(false);
                    digitalOut2.Write(false);
                }

                //Pressure Sensor
                Boolean Pressure_Switch = inputPressure.Read();
                String pressure = "Under threshold";
                if (Pressure_Switch)
                {
                    pressure = "Above threshold";
                    _pcm.StopCompressor();
                }
                Debug.Print("Pressure Value: " + pressure);

                //Compressor
                Boolean StartCompressor = _gamepad.GetButton(10); //"START"-Button
                Boolean StopCompressor = _gamepad.GetButton(9); //"BACK"-Button
                if (StartCompressor && (!Pressure_Switch))
                {
                    _pcm.StartCompressor();
                    Debug.Print("StartCompressor");
                }
                if (StopCompressor)
                {
                    _pcm.StopCompressor();
                    Debug.Print("StopCompressor");
                }





                Boolean FIRE = _gamepad.GetButton(4); //Y-Button
                Boolean Deadman_Switch = inputDead.Read();
                String dead = "OFF";
                if (Deadman_Switch)
                {
                    dead = "ON";
                }
                //Debug.Print("Button Value: " + dead);
                if (FIRE && (!Deadman_Switch))
                {
                    _pcm.SetSolenoidOutput(0, true);
                    solenoidPeriod = (500 * TimeSpan.TicksPerMillisecond) + DateTime.Now.Ticks;
                    Debug.Print("FIRE");
                    SolenoidTimer = true;
                }
                if (SolenoidTimer)
                {
                    long nowSolenoid = DateTime.Now.Ticks;
                    if (nowSolenoid > solenoidPeriod)
                    {
                        _pcm.SetSolenoidOutput(0, false);
                        SolenoidTimer = false;
                        Debug.Print("Close Solenoid");
                    }
                }


                float LeftY = _gamepad.GetAxis(1); //Left Joystick
                float RightY = _gamepad.GetAxis(3); //Right Joystick

                tal1.Set(ControlMode.PercentOutput, LeftY * -1); //moving tal1 with left joystick
                                                                 //   Debug.Print("Left Axis: " + LeftY);
                                                                 //  Debug.Print("Right Axis: " + RightY);
                tal2.Set(ControlMode.PercentOutput, RightY); //moving tal2 with right joystick

                System.Threading.Thread.Sleep(10);

            }
        }

        public static void Main()
        {
            new Program().RunForever();
        }
    }
}
