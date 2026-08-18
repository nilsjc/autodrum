#include <xc.h>

// Konfigurationsbitar (Viktigt för att PICen ska starta!)
#pragma config FOSC = INTRCIO   // Internoscillator, inga externa kristaller
#pragma config WDTE = OFF       // Watchdog Timer avstängd
#pragma config PWRTE = OFF      // Power-up Timer avstängd
#pragma config MCLRE = ON       // Master Clear pinne aktiv
#pragma config CP = OFF         // Kodskydd av
#pragma config CPD = OFF        // Dataskydd av
#pragma config BOREN = OFF      // Brown-out Reset av
#pragma config IESO = OFF       // Internal External Switchover av
#pragma config FCMEN = OFF      // Fail-Safe Clock Monitor av

// Berätta för XC8 vilken hastighet internoscillatorn har (för __delay_ms)
#define _XTAL_FREQ 4000000      // 4 MHz (justeras via OSCCON nedan)

// Ändrat till unsigned int (16 bitar i XC8) för att matcha dina 16-bitars mönster
unsigned int kick    = 0b1010010001000000;
unsigned int hihat   = 0b1000100010101010;
unsigned int snare   = 0b0000100000001001;
unsigned int hiCongo = 0b1010100101010000;
unsigned int loCongo = 0b0101010010101000;
unsigned int volatile check = 0b1000000000000000;

unsigned char GetPotValue(char channel);
unsigned char checkAndDrum(void);

// Variabler för att hålla koll på inkommande data
volatile unsigned char rx_buffer[6];
volatile unsigned char rx_index = 0;
volatile unsigned char rx_state = 0; // 0 = väntar på 'R', 1 = tar emot data
unsigned int pulse_counter = 0; // Håller koll på hur länge pulsen varit hög


int main(void) {
    // 1. Ställ in internoscillatorn till 4 MHz
    OSCCON = 0b01100000; // Bit 6-4 ställer in frekvensen (011 = 4MHz, 001 = 125kHz)
    
    // 2. Ställ in I/O-pinnar (0 = Utgång, 1 = Ingång)
    TRISA = 0b00000001;  // RA0 är ingång (potentiometer)
    TRISC = 0x00;        // Hela PORTC är utgångar till trummorna
    
    // 3. Stäng av analoga funktioner på utgångarna och aktivera på AN0
    ANSEL = 0b00000001;  // Endast ANS0 (RA0) är analog, resten digitala
    ANSELH = 0x00;       // Stäng av analoga funktioner på PORTB/PORTC
    
    // 4. Konfigurera A/D-omvandlaren
    ADCON0bits.CHS = 0;  // Välj kanal AN0
    ADCON0bits.ADON = 1; // Starta A/D-modulen
    ADCON0bits.ADFM = 0; // Vänsterjusterad (MSB i ADRESH)

    // 5. Ställ in RX/TX-pinnar på PIC16F690 (RB5 = RX, RB7 = TX)
    TRISBbits.TRISB5 = 1; // RX måste vara ingång

    // 6. Konfigurera EUSART för t.ex. 9600 Baud (vid 4 MHz klocka)
    TXSTAbits.BRGH = 1;   // High speed baud rate
    SPBRG = 25;           // 25 ger ~9600 baud med 4MHz klocka
    RCSTAbits.CREN = 1;   // Aktivera kontinuerlig mottagning
    RCSTAbits.SPEN = 1;   // Aktivera serieporten

    // 7. Aktivera Interrupts
    PIE1bits.RCIE = 1;    // Aktivera avbrott för serieportsmottagning (Receive Interrupt)
    INTCONbits.PEIE = 1;  // Aktivera perifera avbrott
    INTCONbits.GIE = 1;   // Aktivera globala avbrott


    while(1) {
        // Hämta ett trumsteg och skriv direkt till PORTC
        PORTC = checkAndDrum();
        pulse_counter = 0;

        // Hämta pot-värde (0-255). Ju högre spänning, desto längre delay = långsammare tempo.
        unsigned char pot = GetPotValue(0);
        
        // Skapa en skalbar delay baserad på potentiometern
        // Om pot är 0 lägger vi till 10ms så att det inte blir totalstopp
        for(unsigned int i = 0; i < (pot + 10); i++) {
            __delay_ms(2); // Kort basdelay som repeteras baserat på pot-värdet
            pulse_counter++;
        
            // Efter t.ex. 10 varv (10 * 2ms = 100ms) stänger vi av utgångarna!
            // Processorn fortsätter ändå att räkna resten av tiden för tempot.
            if (pulse_counter == 8) {
                PORTC = 0; 
            }
        }
    }
    return 0;
}

unsigned char checkAndDrum(void) {
    unsigned char output = 0; // Nollställs inför VARJE nytt trumsteg!
    if ((hiCongo & check) > 0) output |= 0b10000; // RC4 = hiCongo
    if ((loCongo & check) > 0) output |= 0b01000; // RC3 = loCongo
    if ((snare   & check) > 0) output |= 0b00100; // RC2 = Snare
    if ((kick    & check) > 0) output |= 0b00010; // RC1 = Kick
    if ((hihat   & check) > 0) output |= 0b00001; // RC0 = Hihat
    
    // Gå till nästa bit/steg i mönstret
    check = check >> 1;
    
    // Om vi har skiftat ut ur 16-bitarsregistret, starta om från början
    if (check == 0) {
        check = 0b1000000000000000;
    }
    
    return output;
}

unsigned char GetPotValue(char channel) {
    ADCON0bits.CHS = channel;
    
    // Liten mjukvarudelay så att A/D-kondensatorn hinner laddas upp (Acquisition time)
    for(volatile char shortDelay = 0; shortDelay < 20; shortDelay++); 
    
    ADCON0bits.GO_DONE = 1;     // Starta omvandling
    while(ADCON0bits.GO_DONE);   // Vänta tills klar
    
    return ADRESH;              // Returnera 8-bitarsvärdet
}

void __interrupt() my_isr(void) {
    // Kontrollera om avbrottet kommer från serieporten (mottagen byte)
    if (PIR1bits.RCIF) {
        
        // Hantera eventuella hårdvarufel (Overrun error) för att inte låsa porten
        if (RCSTAbits.OERR) {
            RCSTAbits.CREN = 0;
            RCSTAbits.CREN = 1;
        }
        
        unsigned char incoming = RCREG; // Läs av den mottagna byten (återställer RCIF)

        if (rx_state == 0) {
            if (incoming == 'R') { // Starttecken hittat!
                rx_state = 1;
                rx_index = 0;
            }
        } 
        else if (rx_state == 1) {
            rx_buffer[rx_index++] = incoming;
            
            // När vi har fått alla 6 bytes, pussla ihop dem till dina 16-bitars mönster
            if (rx_index >= 6) {
                // Skapa 16-bitars värden från två 8-bitars bytes
                kick  = ((unsigned int)rx_buffer[0] << 8) | rx_buffer[1];
                hihat = ((unsigned int)rx_buffer[2] << 8) | rx_buffer[3];
                snare = ((unsigned int)rx_buffer[4] << 8) | rx_buffer[5];
                
                rx_state = 0; // Nollställ och vänta på nästa sändning
            }
        }
    }
}
