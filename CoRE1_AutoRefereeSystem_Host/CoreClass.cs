using System;


namespace CoRE1_AutoRefereeSystem_Host
{
    /*
        変更点
        ・スポットの配列の要素数が3→5
        ・赤青陣地は中立の概念がなくなったので0スタートに変更
        ・タイマーは元々カウントダウンの関係で+5秒であったが、インジケータ追加により、+25秒にする
          (予選なら2:25，決勝なら5:25)
    　　・ストライダ追加により、RobotClassの要素数が12→14に変更
        　ストライダはRed7、Blue7固定
        ・RobotClassにBankerを追加(バンカーに乗っている間1)
        ・RedStrider, BlueStriderを追加(無敵中1)
        ・RedInfTime, BlueInfTimeを追加(残り無敵時間)
     */
    public class CoreClass
    {
        public String GameTime { get; set; } = "00:00";
        //public String SettingTime { get; set; } = "00:00";
        public int GameSystem { get; set; } = 0;    //0:なし，1:予選，2:準決，3:決勝
        //public int GameStatus { get; set; } = 0;    //0:なし，1:セッティング，2:試合前，3:試合中，4:試合終了
        public int RedDeathCnt { get; set; } = 0;   //赤が死んだ回数
        public int BlueDeathCnt { get; set; } = 0;  //青が死んだ回数
        public int RedReceivedDamage { get; set; } = 0;     //赤が受けたダメージ(青が与えたダメージ)
        public int BlueReceivedDamage { get; set; } = 0;    //青が受けたダメージ(赤が与えたダメージ)

        // スポット
        public int[] RedSpot { get; set; } = { 0, 0, 0, 0, 0 };    //0-1, 3-4:攻撃力バフ，[2]:回復
        public int[] BlueSpot { get; set; } = { 0, 0, 0, 0, 0 };

        // ストライダ
        public int RedStrider { get; set; } = 0;
        public int BlueStrider { get; set; } = 0;
        public String RedInfTime { get; set; } = "00";
        public String BlueInfTime { get; set; } = "00";

        //陣地
        public int RedArea { get; set; } = 0;      //0～10：赤
        public int BlueArea { get; set; } = 0;      //0～10：青
        public int CenterArea { get; set; } = 5;    //0～4:赤，5：中立，6～10：青

        // 試合結果
        public uint RedWin { get; set; } = 0;
        public uint BlueWin { get; set; } = 0;
        public uint Winner { get; set; } = 0;  //1:赤, 2:青, 3:引き分け

        public RobotClass[] Robot { get; set; } = { new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass() };
    }

    public class RobotClass
    {

        public int TeamID { get; set; } = 0;
        public String TeamColor { get; set; } = "";
        public int HP { get; set; } = 100;
        public int MaxHP { get; set; } = 100;
        public int DeathFlag { get; set; } = 0;     //0:Alive, 1:Dead(Respawn), 2:Dead
        public String RespawnTime { get; set; } = "00:00";
        public int Banker { get; set; } = 0;


    }

    public class WindowClass
    {
        public string[] TeamName = {   "",
                                        "AGSR [ATK]",
                                        "AVNT [ATK]",
                                        "KNIT [ATK]",
                                        "MKNG [ATK]",
                                        "RTUS [ATK]",
                                        "SEC [ATK]",
                                        "TMC [ATK]",
                                        "TKG [ATK]",
                                        "TRU [ATK]",
                                        "CNTN [ATK]",
                                        "DTB [ATK]",
                                        "TSTM [ATK]",
                                        "KSHH [ATK]",
                                        "DRBS [ATK]",
                                        "RISN [ATK]",
                                        "KSKN [BLD]",
                                        "AGSR [BLD]",
                                        "TRU [BLD]",
                                        "RISN [BLD]",
                                        "SEC [ATR]",
                                        "TKG [ATR]",
                                        "DTB [ATR]",
                                        "KNIT [STR]",
                                        "MKNG [STR]",
                                        ""};


        public string[] Status = { "", "Setting Time", "Stand-by", "Game", "Game Set" };
        // 各ロボットの情報
        public RobotClass[] Red { get; set; } = { new RobotClass(), new RobotClass(), new RobotClass(), new RobotClass(), new RobotClass(), new RobotClass(), new RobotClass() };
        public RobotClass[] Blue { get; set; } = { new RobotClass(), new RobotClass(), new RobotClass(), new RobotClass(), new RobotClass(), new RobotClass(), new RobotClass() };


        // タイマー
        public String GameTime { get; set; } = "00:00";
        public int iGameTime { get; set; } = 0;
        //public String SettingTime { get; set; } = "00:00";
        public int GameSystem { get; set; } = 0;    //0：なし，1:予選，2：準決，3:決勝
        //public int GameStatus { get; set; } = 0;    //0：なし，1：セッティング，2：試合前，3：試合中，4：試合終了
        public int RedDeathCnt { get; set; } = 0;
        public int BlueDeathCnt { get; set; } = 0;
        public int RedReceivedDamage { get; set; } = 0;
        public int BlueReceivedDamage { get; set; } = 0;

        public uint ShowFlag { get; set; } = 0;     // 表示切り替えフラグ

        // 試合結果
        public uint RedWin { get; set; } = 0;
        public uint BlueWin { get; set; } = 0;
        public uint Winner { get; set; } = 0;  //1:赤, 2:青, 3:引き分け


        // スポット
        public int[] RedSpot { get; set; } = { 0, 0, 0, 0, 0 };    //0-1, 3-4:攻撃力バフ，[2]:回復
        public int[] BlueSpot { get; set; } = { 0, 0, 0, 0, 0 };

        // ストライダ
        public int RedStrider { get; set; } = 0;
        public int BlueStrider { get; set; } = 0;
        public String RedInfTime { get; set; } = "00";
        public String BlueInfTime { get; set; } = "00";

        //陣地
        public int RedArea { get; set; } = 0;      //0～4:赤，5：中立，6～10：青
        public int BlueArea { get; set; } = 0;
        public int CenterArea { get; set; } = 5;

    }
    public class PlayerClass
    {
        //画面を操作するプレーヤー
        public bool PlayerTeamColor { get; set; } = false;  //True:Red False:Blue
        public uint PlayerNo { get; set; } = 0;
        public uint EnablePlayerGUI { get; set; } = 0;    // チーム情報の表示選択
        public uint EnableTeamGUI { get; set; } = 0;    // チーム情報の表示選択
        public uint EnableVisible { get; set; } = 0;    // 表示有無
        public uint SettingChengeFlag { get; set; } = 0;     // 表示切り替えフラグ
        public bool EnableCamera { get; set; } = false;    //カメラの表示選択
        public int CameraIndex { get; set; } = 0; //使用するカメラ番号
    }
}