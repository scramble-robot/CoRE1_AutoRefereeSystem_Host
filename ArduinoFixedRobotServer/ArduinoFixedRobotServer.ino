#include <SPI.h>
#include <Ethernet.h>

byte mac[] = { 0xA8, 0x61, 0x0A, 0xAE, 0xE1, 0xA4 };
IPAddress ip(192, 168, 11, 200);
IPAddress myDns(192, 168, 11, 1);
IPAddress gateway(192, 168, 11, 1);
IPAddress subnet(255, 255, 255, 0);

// ポート8888でサーバ開始
EthernetServer server(8888);
EthernetClient client;

const int BUFFER_SIZE = 256;
char recvBuffer[BUFFER_SIZE];
int bufferIndex = 0;
bool waitingForLF = false;
bool timeoutEnabled = false;

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
    
    // 16進数のデータフィールドは全部で8項目
    const int numFields = 8;
    int fields[numFields];
    
    // strtok() は入力文字列を書き換えるのでコピーを作成
    char dataFieldsCopy[BUFFER_SIZE];
    strncpy(dataFieldsCopy, dataFieldsStr, BUFFER_SIZE);
    dataFieldsCopy[BUFFER_SIZE - 1] = '\0';
    
    char *token = strtok(dataFieldsCopy, ",");
    int i = 0;
    while (token != NULL && i < numFields) {
      fields[i] = (int)strtol(token, NULL, 16);
      token = strtok(NULL, ",");
      i++;
    }
    if (i != numFields) {
      Serial.println("Invalid number of data fields");
      return;
    }
    
    // --- DPヒット判定（フィールド index 5）の仮処理 ---
    // 現状はランダム値を設定。将来的に割り込み等で更新する予定
    fields[5] = random(0, 64);
    
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
    
    timeoutEnabled = true;

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

void setup() {
  // 使用するCSピンの設定
  Ethernet.init(10);
  Ethernet.begin(mac, ip, myDns, gateway, subnet);

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
  
  server.begin();
  Serial.print("Server IP: ");
  Serial.println(Ethernet.localIP());
  
  // DPヒット判定のランダム化用に乱数の種を設定
  randomSeed(analogRead(0));
}

void loop() {
  EthernetClient newClient = server.accept();
  if (newClient) {
      Serial.println("We have a new client");
      newClient.write("*** autoRef HOST ***\r\nWelcome to Arduino server! \r\n[OK]\r\n>");
    client = newClient;
  }

  if (client) {
    // timeout
    unsigned long timeout = millis() + 1000;

    while (client.connected() && (millis() < timeout || !timeoutEnabled)) {
      while (client.available() > 0) {
        timeout = millis() + 1000;
        char c = client.read();

        if (waitingForLF) {
          if (c == '\n') {
            // CRLF検出 → コマンド完了
            recvBuffer[bufferIndex] = '\0';
            processCommand(recvBuffer);
            bufferIndex = 0;
            waitingForLF = false;
            continue;
          } else {
            // CR の後に LF でなかった場合、CR を文字として追加
            if (bufferIndex < BUFFER_SIZE - 1) {
              recvBuffer[bufferIndex++] = '\r';
            }
            waitingForLF = false;
          }
        }
        
        if (c == '\r') {
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
    }

    bufferIndex = 0;
    client.stop();
    Serial.println("Client disconnected");
  }
}
