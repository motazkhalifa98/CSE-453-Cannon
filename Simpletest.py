# Simple example of reading the MCP3008 analog input channels and printing
# them all out.
# Author: Tony DiCola
# License: Public Domain
import time

# Import SPI library (for hardware SPI) and MCP3008 library.
import Adafruit_GPIO.SPI as SPI
import Adafruit_MCP3008


import RPi.GPIO as GPIO

import Tkinter as tk

GPIO.setmode(GPIO.BCM)
GPIO.setwarnings(False)
GPIO.setup(26,GPIO.OUT)

# Software SPI configuration:
CLK  = 18
MISO = 23
MOSI = 24
CS   = 25
mcp = Adafruit_MCP3008.MCP3008(clk=CLK, cs=CS, miso=MISO, mosi=MOSI)

# Hardware SPI configuration:
# SPI_PORT   = 0
# SPI_DEVICE = 0
# mcp = Adafruit_MCP3008.MCP3008(spi=SPI.SpiDev(SPI_PORT, SPI_DEVICE))
def setComp(pressure):
    MaxPressure = 80
    CurrentPressure = pressure
    if CurrentPressure>MaxPressure:
        GPIO.output(26,GPIO.HIGH)
    else:
        GPIO.output(26,GPIO.LOW)

def createWindow(window, strVal):
    window.title("Pressure Window")
    window.geometry("1000x1000")
    
    label = tk.Label(window, textvariable=strVal)
    label.pack(padx=100, pady=150)
    
    strVal.set("Pressure: ")

def checkPressure(strVal):
    # Read all the ADC channel values in a list.
    voltage = 0
    current = 0
    pressure = 0
    divtest = 500.0/1000.0
    values = [0]*8
    for i in range(8):
        # The read_adc function will get the value of the specified channel (0-7).
        values[i] = mcp.read_adc(i)
        if (i == 1):
            voltage = (values[1]/1023.0)*3.3
            current = voltage /46.4
            pressure = (current - .004)*100/.016
    strVal.set("pressure: " + str(pressure))
    setComp(pressure)

def update():
    checkPressure(strVal)
    win.after(1000, update)

win = tk.Tk()
strVal = tk.StringVar()
createWindow(win, strVal)
update()
tk.mainloop()
