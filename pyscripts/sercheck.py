# pip install pyserial

import serial
import serial.tools.list_ports
import time

# 1. Hitta tillgängliga COM-portar
ports = list(serial.tools.list_ports.comports())
ftdi_port = None

for p in ports:
    if "FTDI" in p.description or "USB Serial Port" in p.description:
        ftdi_port = p.device
        print(f"Hittade adapter på port: {ftdi_port}")

if not ftdi_port:
    print("Ingen FTDI-adapter hittades i systemet. Avbryter.")
    exit()

# 2. Testa kommunikation (Loopback)
print(f"Öppnar {ftdi_port} för loopback-test (Koppla ihop TX och RX!)...")
try:
    with serial.Serial(ftdi_port, 9600, timeout=1) as ser:
        time.sleep(2) # Låt porten stabiliseras
        
        test_meddelande = b"FT232RL_OK\n"
        ser.write(test_meddelande)
        
        # Läs tillbaka datan
        svar = ser.readline()
        
        if svar == test_meddelande:
            print(" Framgång! Adaptern skickade och tog emot data korrekt.")
        else:
            print(" Fel: Svar matchade inte skickat data. Är TX och RX sammankopplade?")
except Exception as e:
    print(f"Kunde inte kommunicera med porten: {e}")
