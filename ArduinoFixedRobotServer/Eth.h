/* mac address ------------------------------------------------------------------*/
byte mac[] = { 0xA8, 0x61, 0x0A, 0xAE, 0x2E, 0x64 };

// ポート8888でサーバ開始
EthernetServer server(8888);
EthernetClient client;

const int BUFFER_SIZE = 256;
char recvBuffer[BUFFER_SIZE];
int bufferIndex = 0;
bool waitingForLF = false;
bool timeoutEnabled = false;
const byte AddressSW[8] = {A8, A9, A10, A11, A12, A13, A14, A15};

void EthInit(char address);
void processCommand(char *command);

void EthInit(void){

  // DipSWの設定
  int address = 0;
  for (int i = 0; i < 8; i++){
    pinMode(AddressSW[i], INPUT);
    address |= ((!digitalRead(AddressSW[i]) & 0x01) << i);

  }

  // IPAddress ip(192, 168, 11, address);
  // IPAddress myDns(192, 168, 11, 1);
  // IPAddress gateway(192, 168, 11, 1);
  // IPAddress subnet(255, 255, 255, 0);

  IPAddress ip(192, 168, 10, address);
  IPAddress myDns(192, 168, 10, 1);
  IPAddress gateway(192, 168, 10, 1);
  IPAddress subnet(255, 255, 255, 0);
  
  // 使用するCSピンの設定
  Ethernet.init(10);
  Ethernet.begin(mac, ip, myDns, gateway, subnet);


  
  if (Ethernet.hardwareStatus() == EthernetNoHardware) {
    Serial.println("Ethernet shield not found.");
    //while (true) { delay(1); }
  }
  delay(1000);
  if (Ethernet.linkStatus() == LinkOFF) {
    Serial.println("Ethernet cable is not connected.");
  }
  
  server.begin();
  Serial.print("Server IP: ");
  Serial.println(Ethernet.localIP());
}

void processCommand(char *command) {
  // コマンドの種類に応じた処理分岐
  if (strncmp(command, "send ", 5) == 0) {
    // --- send コマンドの処理 ---
    // 1行目：受信文字列をそのままエコー (CRLF付き)
    char response1[BUFFER_SIZE];
    sprintf(response1, "%s\r\n", command);
    
    // "send " の後ろから処理開始
    char *p = command + 5;
    
    // コマンド番号の取得
    char *cmdNumber = strtok(p, " ");
    if (cmdNumber == NULL) {
      Serial.println("Command number missing");
      return;
    }
    
    // データフィールド文字列の取得（例："07,01,01,00,64,00,00,00"）
    char *dataFieldsStr = strtok(NULL, " ");
    if (dataFieldsStr == NULL) {
      Serial.println("Data fields missing");
      return;
    }
  
    // fields
    // strtok() は入力文字列を書き換えるのでコピーを作成
    char dataFieldsCopy[BUFFER_SIZE];
    strncpy(dataFieldsCopy, dataFieldsStr, BUFFER_SIZE);
    dataFieldsCopy[BUFFER_SIZE - 1] = '\0';
    
    char *token = strtok(dataFieldsCopy, ",");

    int CountCheck = 0;
    while (token != NULL && CountCheck < numFields) {
      fields[CountCheck] = (int)strtol(token, NULL, 16);
      token = strtok(NULL, ",");
      CountCheck++;
    }
    if (CountCheck != numFields) {
      Serial.println("Invalid number of data fields");
      return;
    }
    
    // --- 種別（フィールド index 0）の処理 ---
    hostData.iType = fields[0];
    if(DEBUG){
      Serial.print("Type:");
      Serial.println(hostData.iType, DEC);
    }

    // --- フラグ（フィールド index 1）の処理 ---
    hostData.iActiveFlag = fields[1] & 0x01;
    hostData.iDeadFlag = (fields[1] >> 1) & 0x01;
    if(DEBUG){
      Serial.print("Active:");
      Serial.print(hostData.iActiveFlag, DEC);
      Serial.print(" Dead:");
      Serial.println(hostData.iDeadFlag, DEC);
    }

    // --- HP,DPカラー（フィールド index 2）の処理 ---
    hostData.iHPcolor = fields[2] & 0x0f;
    hostData.iDPcolor = (fields[2] >> 4) & 0x0f;
    if(DEBUG){
      //Serial.print("Color ");
      //Serial.print("fields[2]:");
      //Serial.print(fields[2], HEX);
      Serial.print(" HP Color:");
      Serial.print(hostData.iHPcolor, HEX);
      Serial.print(" Panel Color:");
      Serial.println(hostData.iDPcolor, HEX);
    }
    // --- 無敵（フィールド index 3）の処理 ---
    hostData.iInvincible = fields[3];

    // --- HP（フィールド index 4）の処理 ---
    hostData.iHP = fields[4];
    if(DEBUG){
      Serial.print("HP:");
      Serial.println(hostData.iHP, DEC);
    }

    // --- DPヒット判定（フィールド index 5）の仮処理 ---
    // 現状はランダム値を設定。将来的に割り込み等で更新する予定
    //fields[5] = random(0, 64);
    fields[5] = 0;
    for (int i = 0; i < 6; i++){
      if(DamageCnt[i] > 0){
        fields[5] |= (1 << i);
        DamageCnt[i]--;
      }
    }
    
    // 修正後のデータフィールド文字列を作成
    char dataOut[128];
    dataOut[0] = '\0';
    for (int j = 0; j < numFields; j++) {
      char temp[8];
      sprintf(temp, "%02X", fields[j]);
      strcat(dataOut, temp);
      if (j < numFields - 1) {
        strcat(dataOut, ",");
      }
    }
    
    // 2行目：修正後のデータフィールド文字列（CRLF付き）
    char response2[BUFFER_SIZE];
    sprintf(response2, "%s\r\n", dataOut);
    
    // まとめて応答：エコーと修正後データ、その後 "[OK]" とプロンプト ">" を送信
    char combinedResponse[512];
    snprintf(combinedResponse, sizeof(combinedResponse), "%s%s[OK]\r\n>", response1, response2);
    client.write((const uint8_t*)combinedResponse, strlen(combinedResponse));
    
    // デバッグ用出力
    Serial.print("Echoed send command: ");
    Serial.println(response1);
    Serial.print("Sending data: ");
    Serial.println(dataOut);
    Serial.println("------------------------");
    
    //timeoutEnabled = true;

  } else if (strncmp(command, "ping ", 5) == 0) {
    // --- ping コマンドの処理 ---
    // 将来的に "autoturret" や "redbase", "bluebase", "commonbase" 等の分岐を追加可能
    char combinedResponse[512];
    snprintf(combinedResponse, sizeof(combinedResponse), "%s\r\n[OK]\r\n>", command);
    client.write((const uint8_t*)combinedResponse, strlen(combinedResponse));
    Serial.print("Processed ping command: ");
    Serial.println(command);

    timeoutEnabled = false;

  } else if (strncmp(command, "boot ", 5) == 0) {
    // --- boot コマンドの処理 ---
    // 将来的に機能追加可能なようにプレースホルダを用意
    char combinedResponse[512];
    snprintf(combinedResponse, sizeof(combinedResponse), "%s\r\n[OK]\r\n>", command);
    client.write((const uint8_t*)combinedResponse, strlen(combinedResponse));
    Serial.print("Processed boot command: ");
    Serial.println(command);

    timeoutEnabled = false;
    
  } else if (strncmp(command, "shutdown", 8) == 0) {
    // --- shutdown コマンドの処理 ---
    // shutdown はパラメータ無しの想定。将来的に処理内容を追加可能
    char combinedResponse[512];
    snprintf(combinedResponse, sizeof(combinedResponse), "%s\r\n[OK]\r\n>", command);
    client.write((const uint8_t*)combinedResponse, strlen(combinedResponse));
    Serial.print("Processed shutdown command: ");
    Serial.println(command);

    timeoutEnabled = false;
    
  } else {
    // --- 未対応のコマンドの場合 ---
    char combinedResponse[512];
    snprintf(combinedResponse, sizeof(combinedResponse), "%s\r\n[NG]\r\n>", command);
    client.write((const uint8_t*)combinedResponse, strlen(combinedResponse));
    Serial.print("Unsupported command received: ");
    Serial.println(command);

    timeoutEnabled = true;
  }
}