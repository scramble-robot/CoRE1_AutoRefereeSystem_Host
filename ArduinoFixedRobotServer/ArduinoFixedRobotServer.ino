/* Includes ------------------------------------------------------------------*/
#include <SPI.h>
#include <Ethernet.h>
#include <Adafruit_NeoPixel.h>
#include <MsTimer2.h>
#include "valiables.h"
#include "DamagePanel.h"
#include "Eth.h"



void setup() {

  Serial.begin(115200);
  while (!Serial) { ; }

  DamagePanelInit();
  EthInit();

  EthernetClient newClient = server.accept();
  if (newClient) {
      Serial.println("We have a new client");
      newClient.write("*** autoRef HOST ***\r\nWelcome to Arduino server! \r\n[OK]\r\n>");
    client = newClient;
  }
  hostData.iDPcolor = 1;
  hostData.iHPcolor = 1;
  hostData.iHP = 100;
}

void loop() {
  
  if (client) {
    if (client.connected() && (millis() < timeout || !timeoutEnabled)) {
      if (client.available() > 0) {
        timeout = millis() + 1000;
        char c = client.read();

        if (waitingForLF) {
          if (c == '\n') {
            // CRLF検出 → コマンド完了
            recvBuffer[bufferIndex] = '\0';
            processCommand(recvBuffer);
            bufferIndex = 0;
            waitingForLF = false;
          } else {
            // CR の後に LF でなかった場合、CR を文字として追加
            if (bufferIndex < BUFFER_SIZE - 1) {
              recvBuffer[bufferIndex++] = '\r';
            }
            waitingForLF = false;
          }
        } else if (c == '\r') {
          waitingForLF = true;
        } else {
          if (bufferIndex < BUFFER_SIZE - 1) {
            recvBuffer[bufferIndex++] = c;
          } else {
            // バッファオーバーフロー時はリセット
            bufferIndex = 0;
          }
        }
      }
    }else{
      bufferIndex = 0;
      client.stop();
      Serial.println("Client disconnected");
      timeoutEnabled = false;

      delay(50);

      EthernetClient newClient = server.accept();
      if (newClient) {
          Serial.println("We have a new client");
          newClient.write("*** autoRef HOST ***\r\nWelcome to Arduino server! \r\n[OK]\r\n>");
        client = newClient;
      }
      delay(50);
      timeout = millis() + 1000;
    }

  }else{
    bufferIndex = 0;
    timeoutEnabled = false;
    EthernetClient newClient = server.accept();
    if (newClient) {
        Serial.println("We have a new client");
        newClient.write("*** autoRef HOST ***\r\nWelcome to Arduino server! \r\n[OK]\r\n>");
      client = newClient;

      //delay(2000);
      timeout = millis() + 1000;
    }
    
  }

  // ダメージパネルの処理
  if(client){
    for(int i = 0; i < 6; i++){
      if(PanelHitFlg[i] == true){
        delay(10);

        // 割り込み後ダメージパネルが反応していない
        if((digitalRead(PanelPin[i]) == 1) ){
          hitProcess(i);
        }
        PanelHitFlg[i] = false;
      }else{
        for (uint8_t num = 0; num < PANEL_LED_NUM; num++) {
          panelLED[i].setPixelColor(num, DPLEDColor[hostData.iDPcolor]);
        }
        panelLED[i].show();
      }
    }
    // HPの処理
    for(int i = 0; i < 2; i++){
      for (uint8_t num = 0; num < HP_LED_NUM; num++) {
        if(num < (double(HP_LED_NUM) * (double(hostData.iHP) / 100.0))){
          hpLED[i].setPixelColor(num, HPLEDColor[hostData.iHPcolor]);
        }else{
          hpLED[i].setPixelColor(num, HPLEDColor[0]);
        }
      }
      hpLED[i].show();
    }
  }else{
    hostData.iType = 0;
    hostData.iActiveFlag = 1;
    //hostData.iDeadFlag = 0;
    hostData.iHPcolor = 2;
    hostData.iDPcolor = 2;
    //hostData.iHP = 100;
    hostData.iInvincible = 0;

    for(int i = 0; i < 6; i++){
      if(PanelHitFlg[i] == true){
        delay(10);

        // 割り込み後ダメージパネルが反応していない
        if((digitalRead(PanelPin[i]) == 1) ){
          hitProcess(i);
        }
        PanelHitFlg[i] = false;
      }else{
        for (uint8_t num = 0; num < PANEL_LED_NUM; num++) {
          panelLED[i].setPixelColor(num, DPLEDColor[2]);
        }
        panelLED[i].show();
      }
    }
    hostData.iHP = 100 - (DamageCnt[0] * 10);
    if(hostData.iHP == 0){
      hostData.iDeadFlag = 1;
    }else{
      hostData.iDeadFlag = 0;
    }

    // HPの処理
    for(int i = 0; i < 2; i++){
      for (uint8_t num = 0; num < HP_LED_NUM; num++) {
        if(num < (double(HP_LED_NUM) * (double(hostData.iHP) / 100.0))){
          hpLED[i].setPixelColor(num, HPLEDColor[2]);
        }else{
          hpLED[i].setPixelColor(num, HPLEDColor[0]);
        }
      }
      hpLED[i].show();
    }
  }

  // 全体処理
  if(hostData.iDeadFlag){
    digitalWrite(DeadPin[0], HIGH);
  }else{
    digitalWrite(DeadPin[0], LOW);
  }
}
