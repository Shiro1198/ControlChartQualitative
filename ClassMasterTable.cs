using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlChart
{
    /// <summary>
    /// コントロールチャートマスタ
    /// </summary>
    public class M_CTRL_CHART
    {
        public string K_CODE { get; set; }      // 項目コード
        public string K_NAME { get; set; }      // 項目名称
        public string K_RYAK { get; set; }      // 項目略称
        public string ASSAY_STYLE { get; set; } // 検査方法
        public string ASSAY_UNIT_NAME { get; set; } // 検査単位名称
        public string VALID_COL { get; set; }       // 有効桁: （AB  A：整数(全体)，B：小数）
        public int MARU_COND { get; set; }          // 丸め条件: （0：四捨五入，1：切捨て，2：切上げ）
        public int GETSEL_KBN { get; set; }         // 取込み選択区分: （0：TOPからN本，1：ランダムにN本）
        public int OUT_INTERVAL { get; set; }       // 出力間隔: （01：毎月，02：1月．3月．5月．・・・・・）
        public int CHART_MODE { get; set; }         // 管理図モード:（1：X-Rs-R図，2：X-R図，3：X-Rs図）
	    public int LINE_MODE { get; set; }          // 管理線モード: （1：計算値，2：管理値）
        public int ASSAY_CYCLE { get; set; }        // 検査サイクル:（検査アッセイサイクルと同じ）
        public int RESRV_CNT { get; set; }          // 累積件数:（出力及び表示件数 (最大62件)）
	    public string MEMO_INF { get; set; }        // 備考
        public DateTime CREATE_DATE { get; set; }   // 登録日
        public DateTime UPDATE_DATE { get; set; }   // 更新日
        public string UP_OPE_CODE { get; set; }     // 更新者
    }

    public class  M_CTRL_TUBE
    {
        public string TUBE_CODE { get; set; }       // チューブコード
	    public string K_CODE { get; set; }          // 項目コード
	    public string MNG_NAME { get; set; }        // 管理試験名称
	    public string MNG_RYAK { get; set; }        // 管理試験略称
	    public string CUTOFF_H { get; set; }        // カットオフ値（上限）
	    public string CUTOFF_L { get; set; }        // カットオフ値（下限）
	    public int CTRLPARM_DISP_SU { get; set; }   // CTRL係数・表示本数（検査対象データ数の本数（N)）
    }
}
