/* defines -------------------------------------------------------------------*/
#define MAX_BRIGHTNESS 128

#define PANEL_LED_NUM 24
#define AREA_LED_NUM 130
#define HP_LED_NUM 20

#define PANEL_DISABLE_TIME 200
#define AREA_NO_DAMAGE_TIME 4500


/* pin assignments -----------------------------------------------------------*/
const byte PanelPin[6] = {3, 2, 18, 19, 20, 21};      //割り込みピン
const byte PanelLEDPin[6] = {26, 27, 28, 29, 32, 33};
const byte DeadPin[2] = {34, 35};
const byte AreaLEDPin[2] = {46, 47};
const byte HpLEDPin[2] = {48, 49};


/* flags ---------------------------------------------------------------------*/
volatile bool PanelHitFlg[6] = {false, false, false, false, false, false};

/* Adafruit_NeoPixel ---------------------------------------------------------*/
Adafruit_NeoPixel panelLED[6] = { Adafruit_NeoPixel(PANEL_LED_NUM, PanelLEDPin[0], NEO_GRB + NEO_KHZ800),
                                  Adafruit_NeoPixel(PANEL_LED_NUM, PanelLEDPin[1], NEO_GRB + NEO_KHZ800),
                                  Adafruit_NeoPixel(PANEL_LED_NUM, PanelLEDPin[2], NEO_GRB + NEO_KHZ800),
                                  Adafruit_NeoPixel(PANEL_LED_NUM, PanelLEDPin[3], NEO_GRB + NEO_KHZ800),
                                  Adafruit_NeoPixel(PANEL_LED_NUM, PanelLEDPin[4], NEO_GRB + NEO_KHZ800),
                                  Adafruit_NeoPixel(PANEL_LED_NUM, PanelLEDPin[5], NEO_GRB + NEO_KHZ800)};
Adafruit_NeoPixel areaLED[2] = {Adafruit_NeoPixel(AREA_LED_NUM, AreaLEDPin[0], NEO_GRB + NEO_KHZ800),
                              Adafruit_NeoPixel(AREA_LED_NUM, AreaLEDPin[1], NEO_GRB + NEO_KHZ800)};
Adafruit_NeoPixel hpLED[2] = {Adafruit_NeoPixel(HP_LED_NUM, HpLEDPin[0], NEO_GRB + NEO_KHZ800),
                              Adafruit_NeoPixel(HP_LED_NUM, HpLEDPin[1], NEO_GRB + NEO_KHZ800)};

/* variables -----------------------------------------------------------------*/
uint32_t DPLEDColor[10] = {}; //0:Clear 1 :Red 2:Green 3:Blue 4:Cyan 5:Magenta 6:Yellow 7:WHITE 8:FULL_WHITE
uint32_t HPLEDColor[10] = {}; //0:Clear 1 :Red 2:Green 3:Blue 4:Yellow 5:White

volatile uint32_t PanelHitTime[6] = {};

/* function prototypes -------------------------------------------------------*/
void DamagePanelInit(void);
void timer(void);
void hit1(void);
void hit2(void);
void hit3(void);
void hit4(void);
void hit5(void);
void hit6(void);
void hitProcess(int num);
void areaLEDProcess(void);

void DamagePanelInit(void){
  // DPセンサの入力設定
  for (int i = 0; i < 6; i++){
    pinMode(PanelPin[i], INPUT_PULLUP);
    PanelHitFlg[i] = false;
  }
  // 撃破信号の出力設定
  for (int i = 0; i < 2; i++){
    pinMode(DeadPin[i], OUTPUT);
  }

  // ダメージパネルの割り込み設定
  attachInterrupt(digitalPinToInterrupt(PanelPin[0]), hit1, RISING);
  attachInterrupt(digitalPinToInterrupt(PanelPin[1]), hit2, RISING);
  attachInterrupt(digitalPinToInterrupt(PanelPin[2]), hit3, RISING);
  attachInterrupt(digitalPinToInterrupt(PanelPin[3]), hit4, RISING);
  attachInterrupt(digitalPinToInterrupt(PanelPin[4]), hit5, RISING);
  attachInterrupt(digitalPinToInterrupt(PanelPin[5]), hit6, RISING);

  // パネル用LEDの初期化
  for (int i = 0; i < 6; i++){
    panelLED[i].begin();
    panelLED[i].show();
  }

  // Area用LEDの初期化
  for (int i = 0; i < 2; i++){
    areaLED[i].begin();
    areaLED[i].show();
  }

  // HP用LEDの初期化
  for (int i = 0; i < 2; i++){
    hpLED[i].begin();
    hpLED[i].show();
  }

  // LEDの色を作成 0:Clear 1 :Red 2:Green 3:Blue 4:Cyan 5:Magenta 6:Yellow 7:WHITE 8:FULL_WHITE
  DPLEDColor[0] = panelLED[0].Color(0, 0, 0);
  DPLEDColor[1] = panelLED[0].Color(MAX_BRIGHTNESS, 0, 0);
  DPLEDColor[2] = panelLED[0].Color(0, MAX_BRIGHTNESS, 0);
  DPLEDColor[3] = panelLED[0].Color(0, 0, MAX_BRIGHTNESS);
  DPLEDColor[4] = panelLED[0].Color(0, 0, 0);
  DPLEDColor[5] = panelLED[0].Color(0, 0, 0);
  DPLEDColor[6] = panelLED[0].Color(MAX_BRIGHTNESS/2, MAX_BRIGHTNESS/2, 0);
  DPLEDColor[7] = panelLED[0].Color(MAX_BRIGHTNESS/3, MAX_BRIGHTNESS/3, MAX_BRIGHTNESS/3);
  DPLEDColor[8] = panelLED[0].Color(MAX_BRIGHTNESS/3, MAX_BRIGHTNESS/3, MAX_BRIGHTNESS/3);

  HPLEDColor[0] = panelLED[0].Color(0, 0, 0);
  HPLEDColor[1] = panelLED[0].Color(MAX_BRIGHTNESS, 0, 0);
  HPLEDColor[2] = panelLED[0].Color(0, MAX_BRIGHTNESS, 0);
  HPLEDColor[3] = panelLED[0].Color(0, 0, MAX_BRIGHTNESS);
  HPLEDColor[4] = panelLED[0].Color(MAX_BRIGHTNESS/2, MAX_BRIGHTNESS/2, 0);
  HPLEDColor[5] = panelLED[0].Color(MAX_BRIGHTNESS/3, MAX_BRIGHTNESS/3, MAX_BRIGHTNESS/3);

  // LEDの色を設定
  for (uint8_t i = 0; i < PANEL_LED_NUM; i++) {
    panelLED[0].setPixelColor(i, DPLEDColor[7]);
    panelLED[1].setPixelColor(i, DPLEDColor[7]);
    panelLED[2].setPixelColor(i, DPLEDColor[7]);
    panelLED[3].setPixelColor(i, DPLEDColor[7]);
    panelLED[4].setPixelColor(i, DPLEDColor[7]);
    panelLED[5].setPixelColor(i, DPLEDColor[7]);

    panelLED[0].show();
    panelLED[1].show();
    panelLED[2].show();
    panelLED[3].show();
    panelLED[4].show();
    panelLED[5].show();

    delay(30);
  }
  for (uint8_t i = 0; i < AREA_LED_NUM; i++) {
    areaLED[0].setPixelColor(i, HPLEDColor[5]);
    areaLED[1].setPixelColor(i, HPLEDColor[5]);

    areaLED[0].show();
    areaLED[1].show();

    delay(5);
  }
  for (uint8_t i = 0; i < HP_LED_NUM; i++) {
    hpLED[0].setPixelColor(i, HPLEDColor[5]);
    hpLED[1].setPixelColor(i, HPLEDColor[5]);

    hpLED[0].show();
    hpLED[1].show();

    delay(30);
  }
  
}

/**
  * @brief 
  * @retval none
  */
void hit1(void) {
  // 陣地とオートタレットで反応時間を分ける
  if( (hostData.iType == 8) || (hostData.iType == 9) ){
    if( ((millis() - PanelHitTime[0]) > AREA_NO_DAMAGE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>0)&0x01)){
      DamageCnt[0]++;
      PanelHitFlg[0] = true;
      PanelHitTime[0] = millis();
    }
  }else{
    if( ((millis() - PanelHitTime[0]) > PANEL_DISABLE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>0)&0x01)){
      DamageCnt[0]++;
      PanelHitFlg[0] = true;
      PanelHitTime[0] = millis();
    }
  }

}


/**
  * @brief 
  * @retval none
  */
void hit2(void) {
  // 陣地とオートタレットで反応時間を分ける
  if( (hostData.iType == 8) || (hostData.iType == 9) ){
    if( ((millis() - PanelHitTime[1]) > AREA_NO_DAMAGE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>1)&0x01)){
      DamageCnt[1]++;
      PanelHitFlg[1] = true;
      PanelHitTime[1] = millis();
    }
  }else{
    if( ((millis() - PanelHitTime[1]) > PANEL_DISABLE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>1)&0x01)){
      DamageCnt[1]++;
      PanelHitFlg[1] = true;
      PanelHitTime[1] = millis();
    }
  }
}


/**
  * @brief 
  * @retval none
  */
void hit3(void) {
  // 陣地とオートタレットで反応時間を分ける
  if( (hostData.iType == 8) || (hostData.iType == 9) ){
    if( ((millis() - PanelHitTime[2]) > AREA_NO_DAMAGE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>2)&0x01)){
      DamageCnt[2]++;
      PanelHitFlg[2] = true;
      PanelHitTime[2] = millis();
    }
  }else{
    if( ((millis() - PanelHitTime[2]) > PANEL_DISABLE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>2)&0x01)){
      DamageCnt[2]++;
      PanelHitFlg[2] = true;
      PanelHitTime[2] = millis();
    }
  }
}


/**
  * @brief 
  * @retval none
  */
void hit4(void) {
  // 陣地とオートタレットで反応時間を分ける
  if( (hostData.iType == 8) || (hostData.iType == 9) ){
    if( ((millis() - PanelHitTime[3]) > AREA_NO_DAMAGE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>3)&0x01)){
      DamageCnt[3]++;
      PanelHitFlg[3] = true;
      PanelHitTime[3] = millis();
    }
  }else{
    if( ((millis() - PanelHitTime[3]) > PANEL_DISABLE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>3)&0x01)){
      DamageCnt[3]++;
      PanelHitFlg[3] = true;
      PanelHitTime[3] = millis();
    }
  }
}

/**
  * @brief 
  * @retval none
  */
void hit5(void) {
  // 陣地とオートタレットで反応時間を分ける
  if( (hostData.iType == 8) || (hostData.iType == 9) ){
    if( ((millis() - PanelHitTime[4]) > AREA_NO_DAMAGE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>4)&0x01)){
      DamageCnt[4]++;
      PanelHitFlg[4] = true;
      PanelHitTime[4] = millis();
    }
  }else{
    if( ((millis() - PanelHitTime[4]) > PANEL_DISABLE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>4)&0x01)){
      DamageCnt[4]++;
      PanelHitFlg[4] = true;
      PanelHitTime[4] = millis();
    }
  }
}

/**
  * @brief 
  * @retval none
  */
void hit6(void) {
  // 陣地とオートタレットで反応時間を分ける
  if( (hostData.iType == 8) || (hostData.iType == 9) ){
    if( ((millis() - PanelHitTime[5]) > AREA_NO_DAMAGE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>5)&0x01)){
      DamageCnt[5]++;
      PanelHitFlg[5] = true;
      PanelHitTime[5] = millis();
    }
  }else{
    if( ((millis() - PanelHitTime[5]) > PANEL_DISABLE_TIME) 
      && hostData.iActiveFlag && !hostData.iDeadFlag && !((hostData.iInvincible>>5)&0x01)){
      DamageCnt[5]++;
      PanelHitFlg[5] = true;
      PanelHitTime[5] = millis();
    }
  }
}

/**
  * @brief 
  * @retval none
  */
void hitProcess(int num){
 
    // ダメージを受けたときに点滅させる
    for(uint8_t cnt = 0; cnt < 2; cnt++){
      // 点滅させるために消灯
      panelLED[num].clear();
      panelLED[num].show();

      delay(50);

      for (uint8_t i = 0; i < PANEL_LED_NUM; i++) {
        panelLED[num].setPixelColor(i, DPLEDColor[hostData.iDPcolor]);
      }
      panelLED[num].show();

      delay(50);
      
    }

}