#include <SPI.h>
#include <Ethernet.h>
#include <EthernetUdp.h>

/*== WARN: Arduinoごとに変更すること！！！ =============*/
byte mac[] = { 0xA8, 0x61, 0x0A, 0xAE, 0x2E, 0x65 };
IPAddress ip(192, 168, 11, 220);
/*====================================================*/

IPAddress remoteIP(192, 168, 11, 10);
unsigned int remotePort = 8888;
unsigned int localPort = 8888;

EthernetUDP Udp;
String sendStr = "R00000B00000\r\n";

void setup() {
  Ethernet.init(10);
  Ethernet.begin(mac, ip);

  Serial.begin(115200);
  while (!Serial) { ; }

  if (Ethernet.hardwareStatus() == EthernetNoHardware) {
    Serial.println("Ethernet shield not found.");
    while (true) { delay(1); }
  }
  delay(1000);
  if (Ethernet.linkStatus() == LinkOFF) {
    Serial.println("Ethernet cable is not connected.");
  }

  Udp.begin(localPort);

  Serial.println("setup finished");
}

void loop() {

  if (Serial.available() > 0) {
    sendStr = Serial.readStringUntil('\n');
    Serial.println(sendStr);
  }

  // UDPパケットの作成
  Udp.beginPacket(remoteIP, remotePort);
  Udp.write(sendStr.c_str(), sendStr.length());
  Udp.endPacket();

  delay(500);
}

