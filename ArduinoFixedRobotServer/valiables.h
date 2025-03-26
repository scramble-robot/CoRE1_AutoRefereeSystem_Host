const bool DEBUG = true;

const int numFields = 8;
int fields[numFields];

struct HostData{
  int iType = 0;
  int iActiveFlag = 1;
  int iDeadFlag = 0;
  int iHPcolor = 2;
  int iDPcolor = 2;
  int iHP = 100;
  int iInvincible = 0;
};
HostData hostData;

int DamageCnt[6] = {};

unsigned long timeout;