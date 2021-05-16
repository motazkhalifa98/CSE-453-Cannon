
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
        SafeOutputPort digitalOut1 = new SafeOutputPort(CTRE.HERO.IO.Port1.Pin4, false);  //Port1, PIN 4 on Hero Board > IN2 in L298 Connected to Linear Actuator
        SafeOutputPort digitalOut2 = new SafeOutputPort(CTRE.HERO.IO.Port1.Pin5, false);   //Port1, PIN 5 on Hero Board > IN1 in L298 Connected to Linear Actuator
        InputPort inputDead = new InputPort(CTRE.HERO.IO.Port6.Pin4, false, Port.ResistorMode.PullDown); //Deadman switch, Port6 Pin4 on Hero Board
        InputPort inputPressure = new InputPort(CTRE.HERO.IO.Port6.Pin5, false, Port.ResistorMode.Disabled); //Input Pressure from Pi Port 26 connected to Port6 Pin5
        TalonSRX tal1 = new TalonSRX(1); //first Talon, ID = 1
        TalonSRX tal2 = new TalonSRX(2);//second Talon, ID = 2
        long solenoidPeriod; //Time to auto-shutdown solenoid valve after shooting/opening it.

        public void RunForever()
        {
            _pcm.SetSolenoidOutput(1, false); //Initialize the compressor to be turned off, compressor ID = 1
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
                    Debug.Print("Not connected: " + i); //The controller is not connected
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
                Boolean Pressure_Switch = inputPressure.Read(); //Input from Raspberry Pi
                String pressure = "Under threshold";
                if (Pressure_Switch)
                {
                    pressure = "Above threshold";
                    _pcm.SetSolenoidOutput(1, false);	//Pressure is above threshold, turn off solenoid
                }
                Debug.Print("Pressure Value: " + pressure); //Print pressure value

                //Compressor
                Boolean StartCompressor = _gamepad.GetButton(10); //"START"-Button
                Boolean StopCompressor = _gamepad.GetButton(9); //"BACK"-Button
                if (StartCompressor && (!Pressure_Switch)) //If pressure is below threshold and "START" is pressed
                {
                    _pcm.SetSolenoidOutput(1, true); //Start compressor
                    Debug.Print("StartCompressor");
                }
                if (StopCompressor) //If "BACK" is pressed
                {
		    _pcm.SetSolenoidOutput(1, false); //Stop compressor
					
                    Debug.Print("StopCompressor");
                }





                Boolean FIRE = _gamepad.GetButton(4); //Y-Button
                Boolean Deadman_Switch = inputDead.Read();
                String dead = "OFF";
                if (Deadman_Switch) //Current state of deadman switch
                {
                    dead = "ON";
                }
                //Debug.Print("Button Value: " + dead);
                if (FIRE && (!Deadman_Switch)) //If Y is pressed and deadman switch is off
                {
                    _pcm.SetSolenoidOutput(0, true); //Open Solenoid/Fire ID = 0 for solenoid
                    solenoidPeriod = (500 * TimeSpan.TicksPerMillisecond) + DateTime.Now.Ticks; //Start timer for half a second
                    Debug.Print("FIRE");
                    SolenoidTimer = true;
                }
                if (SolenoidTimer)
                {
                    long nowSolenoid = DateTime.Now.Ticks;
                    if (nowSolenoid > solenoidPeriod) //If half a second has passed
                    {
                        _pcm.SetSolenoidOutput(0, false); //Close the solenoid
                        SolenoidTimer = false;
                        Debug.Print("Close Solenoid");
                    }
                }

                Boolean LeftForward = _gamepad.GetButton(5); //LB
                Boolean LeftBackward = _gamepad.GetButton(7); //LT
                Boolean RightForward = _gamepad.GetButton(6); //RB
                Boolean RightBackward = _gamepad.GetButton(8); //RT

                float LeftY = 0;
                float RightY = 0;
                //Talon SRX == Drive System
                if (LeftForward)
                {
                    LeftY = (float) (0.5);
                }
                if (LeftBackward)
                {
                    LeftY = (float)(-0.5);

                }
                if (RightForward)
                {
                    RightY = (float)(0.5);
                }
                if (RightBackward)
                {
                    RightY = (float)(-0.5);
                }


                tal1.Set(ControlMode.PercentOutput, LeftY * -1); //moving tal1 with LT & LB
                tal2.Set(ControlMode.PercentOutput, RightY); //moving tal2 with RT & RB

                System.Threading.Thread.Sleep(10);

            }
        }

        public static void Main()
        {
            new Program().RunForever();
        }
    }
}
