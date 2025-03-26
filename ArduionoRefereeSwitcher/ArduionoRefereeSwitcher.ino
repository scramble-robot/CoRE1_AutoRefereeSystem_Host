#include <SPI.h>
#include <Ethernet.h>
#include <EthernetUdp.h>

const byte AddressSW[8] = {A8, A9, A10, A11, A12, A13, A14, A15};
const byte RedSW[12] = {26, 27, 28, 29, 32, 33};
const byte BlueSW[12] = {34, 35, 46, 47, 48, 49};

IPAddress remoteIP(192, 168, 11, 10);
unsigned int remotePort = 8888;
unsigned int localPort = 8888;

EthernetUDP Udp;
String sendStr = "R00000B00000\r\n";

void setup() {
  // DipSWの設定
  int address = 0;
  for (int i = 0; i < 8; i++){
    pinMode(AddressSW[i], INPUT);
    address |= ((!digitalRead(AddressSW[i]) & 0x01) << i);
  }
  // トグルSWの設定
  for (int i = 0; i < 6; i++){
    pinMode(RedSW[i], INPUT);
    pinMode(BlueSW[i], INPUT);
  }

  /*== WARN: Arduinoごとに変更すること！！！ =============*/
  byte mac[] = { 0xA8, 0x61, 0x0A, 0xAE, 0x2E, address };
  IPAddress ip(192, 168, 11, address);
  /*====================================================*/

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
  Serial.print("Server IP: ");
  Serial.println(Ethernet.localIP());
  //Serial.println("setup finished");
}

void loop() {

  if (Serial.available() > 0) {
    sendStr = Serial.readStringUntil('\n');
    Serial.print("Rec: ");
    Serial.println(sendStr);
  }

  // UDPパケットの作成
  for(int i = 0; i < 5; i++){
    if(digitalRead(RedSW[i]) == 0){
      sendStr[5-i] = '1';
    }else{
      sendStr[5-i] = '0';
    }
    if(digitalRead(BlueSW[i]) == 0){
      sendStr[5-i + 6] = '1';
    }else{
      sendStr[5-i + 6] = '0';
    }
  }

  Udp.beginPacket(remoteIP, remotePort);
  Udp.write(sendStr.c_str(), sendStr.length());
  Udp.endPacket();
  Serial.print("Send: ");
  Serial.println(sendStr);

  delay(500);
}

