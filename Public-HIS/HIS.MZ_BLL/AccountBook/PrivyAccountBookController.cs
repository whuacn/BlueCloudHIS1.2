﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using HIS.SYSTEM.Core;
using System.Collections;
using HIS.MZ_BLL.Exceptions;
using HIS.BLL;

namespace HIS.MZ_BLL
{
    /// <summary>
    /// 个人账单控制器
    /// </summary>
    public class AccountBookController : BaseBLL
    {
        /// <summary>
        /// 医院ID
        /// </summary>
        private static int WORK_ID = Convert.ToInt32( HIS.SYSTEM.Core.EntityConfig.WorkID );
        /// <summary>
        /// 获取个人账单
        /// </summary>
        /// <param name="TollCollectorId">收费员ID</param>
        /// <param name="AccountBookId">账单ID</param>
        /// <returns></returns>
        public static PrivyAccountBook GetPrivyAccountBook( int TollCollectorId,int AccountBookId )
        {
            DataTable tbInvoiceList = null;
            DataTable tbInvoiceDetailList = null;
            #region 获取本次收费发票列表
//            //发票列表(所有的发票，包含挂号和收费)
//            string sql = @"select a.patlistid,b.meditype,a.record_flag, ticketnum,total_fee,village_fee,favor_fee,pos_fee,self_tally, 
//                                    (total_fee-village_fee-favor_fee-pos_fee-self_tally) as money_fee ,a.hang_flag,a.costdate
//                            from mz_costmaster a,mz_patlist b 
//                            where a.patlistid=b.patlistid 
//                            and a.record_flag in (0,1,2)
//                            and a.chargecode='" + TollCollectorId.ToString( ) + @"'
//                            and a.accountid = " + AccountBookId + @"
//                            and a.workid=" + WORK_ID + @"
//                            and b.workid= "+ WORK_ID+ @"
//                             order by costdate";
//            tbInvoiceList = oleDb.GetDataTable( sql );
//            //发票明细列表(所有的，包含挂号和收费)
//            sql = @"select a.ticketnum,c.code as stat_item_code,c.mzfp_code,d.item_name, b.total_fee ,a.record_flag,a.hang_flag,
//                        a.village_fee,a.favor_fee,a.pos_fee,a.self_tally, 
//                        (a.total_fee-a.village_fee-a.favor_fee-a.pos_fee-a.self_tally) as money_fee
//                    from mz_costmaster a,mz_costorder b ,base_stat_item c,base_stat_mzfp d 
//                    where a.costmasterid = b.costid 
//                    and b.itemtype=c.code
//                    and c.mzfp_code = d.code 
//                    and a.record_flag in (0,1,2)
//                    and a.chargecode='" + TollCollectorId.ToString( ) + @"'
//                    and a.accountid = " + AccountBookId + @"
//                    and a.workid=" + WORK_ID + @"
//                    and b.workid= "+ WORK_ID;
//            tbInvoiceDetailList = oleDb.GetDataTable( sql );
            #endregion
            GetAccountData( TollCollectorId , new int[] { AccountBookId } , out tbInvoiceList , out tbInvoiceDetailList );
            DataTable tbAccount = oleDb.GetDataTable( "select * from mz_account where accountid=" + AccountBookId + " and workid=" + WORK_ID );
            return GetPrivyAccountBook( TollCollectorId , AccountBookId , tbInvoiceList , tbInvoiceDetailList , tbAccount );
        }
        /// <summary>
        /// 获取个人账单
        /// </summary>
        /// <param name="TollCollectorId">收费员ID</param>
        /// <param name="AccountBookId">账单ID</param>
        /// <param name="tbInvoiceList">发票清单</param>
        /// <param name="tbInvoiceDetailList">发票明细列表</param>
        /// <returns></returns>
        public static PrivyAccountBook GetPrivyAccountBook( int TollCollectorId , int AccountBookId , 
                                                            DataTable tbAllInvoiceList , DataTable tbAllInvoiceDetailList ,DataTable tbAccountList )
        {
            DataTable tbInvoiceList =  tbAllInvoiceList.Clone();
            DataRow[] drsInvoiceList = tbAllInvoiceList.Select( "AccountId=" + AccountBookId );
            foreach ( DataRow dr in drsInvoiceList )
                tbInvoiceList.Rows.Add( dr.ItemArray );

            DataTable tbInvoiceDetailList = tbAllInvoiceDetailList.Clone( );
            DataRow[] drsInvoiceDetailList = tbAllInvoiceDetailList.Select( "AccountId=" + AccountBookId );
            foreach ( DataRow dr in drsInvoiceDetailList )
                tbInvoiceDetailList.Rows.Add( dr.ItemArray );

            DataRow[] drAccounts = tbAccountList.Select( "ACCOUNTID=" + AccountBookId );
            DataRow drAccount = null;
            if ( drAccounts.Length != 0 )
                drAccount = drAccounts[0];

            PrivyAccountBook accountBook = new PrivyAccountBook( );
            if ( drAccount != null )
            {
                accountBook.AccountBookDate = Convert.ToDateTime( drAccount["AccountDate"] );
                accountBook.AccountId = Convert.ToInt32( drAccount["AccountId"] );
                //accountBook.TollCollectorName = PublicDataReader.GetEmployeeNameById( TollCollectorId );
                accountBook.TollCollectorName = BaseDataController.GetName( BaseDataCatalog.人员列表, TollCollectorId );
            }
            else
            {
                accountBook.AccountId = 0;
            }
            #region 发票科目明细列表
            Hashtable htInvoiceItems = new Hashtable( );
            for ( int i = 0 ; i < tbInvoiceDetailList.Rows.Count ; i++ )
            {
                string mzfp_code = tbInvoiceDetailList.Rows[i]["mzfp_code"].ToString( ).Trim( );
                string mzfp_name = tbInvoiceDetailList.Rows[i]["item_name"].ToString( ).Trim( );
                decimal item_fee = Convert.ToDecimal( tbInvoiceDetailList.Rows[i]["total_fee"] );
                InvoiceItem invoiceItem = new InvoiceItem( );
                invoiceItem.ItemCode = mzfp_code;
                invoiceItem.ItemName = mzfp_name;
                invoiceItem.Cost = item_fee;

                if ( htInvoiceItems.ContainsKey( invoiceItem.ItemCode ) )
                {
                    InvoiceItem _invoiceItem = (InvoiceItem)htInvoiceItems[invoiceItem.ItemCode];
                    htInvoiceItems.Remove( invoiceItem.ItemCode );
                    _invoiceItem.Cost = _invoiceItem.Cost + invoiceItem.Cost;
                    htInvoiceItems.Add( _invoiceItem.ItemCode , _invoiceItem );

                }
                else
                {
                    htInvoiceItems.Add( invoiceItem.ItemCode , invoiceItem );
                }
                accountBook.InvoiceItemSumTotal += invoiceItem.Cost;
            }
            accountBook.InvoiceItem = new InvoiceItem[htInvoiceItems.Count];
            int invoiceItemIndex = 0;
            foreach ( object obj in htInvoiceItems )
            {
                accountBook.InvoiceItem[invoiceItemIndex] = (InvoiceItem)( (DictionaryEntry)obj ).Value;
                invoiceItemIndex++;
            }
            #endregion
            //收费票据信息
            AccountBillInfo chargeBillInfo =  GetBillInfo( OPDOperationType.门诊收费 , tbInvoiceList );
            //chargeBillInfo.ChargeName = PublicDataReader.GetEmployeeNameById( TollCollectorId );
            chargeBillInfo.ChargeName = BaseDataController.GetName(BaseDataCatalog.人员列表,TollCollectorId);
            accountBook.ChargeInvoiceInfo = chargeBillInfo;
            //挂号票据信息
            AccountBillInfo registerBillInfo = GetBillInfo( OPDOperationType.门诊挂号 , tbInvoiceList );
            registerBillInfo.ChargeName =  chargeBillInfo.ChargeName;
            accountBook.RegisterInvoiceInfo = registerBillInfo;
            //优惠部分
            accountBook.FavorPart = GetFavorPart( tbInvoiceList );
            //现金部分
            accountBook.CashPart = GetCashPart( tbInvoiceList , tbInvoiceDetailList );
            //记账部分
            accountBook.TallyPart = GetTallyPart( tbInvoiceList );

            return accountBook;
        }
        /// <summary>
        /// 获取指定时间段指定的收费员的交账单
        /// </summary>
        /// <param name="TollCollectorId">收费员ID</param>
        /// <param name="BeginDate">开始时间</param>
        /// <param name="EndDate">结束时间</param>
        /// <returns></returns>
        public static List<PrivyAccountBook> GetPrivyAccountBooks( int TollCollectorId, DateTime BeginDate, DateTime EndDate )
        {
            DataTable tbAccountList = GetAccountList( BeginDate, EndDate );
            DataRow[] drsAccountList = tbAccountList.Select( Tables.mz_account.ACCOUNTID + "='" + TollCollectorId.ToString()  +"'" );
            int[] accountBookid = new int[drsAccountList.Length];
            for ( int i = 0; i < drsAccountList.Length; i++ )
                accountBookid[i] = Convert.ToInt32( drsAccountList[i][Tables.mz_account.ACCOUNTID] );

            DataTable tbInvoiceList;
            DataTable tbInvoiceDetailList;
            GetAccountData( TollCollectorId, accountBookid, out tbInvoiceList, out tbInvoiceDetailList );
            List<PrivyAccountBook> lstBooks = new List<PrivyAccountBook>();
            for ( int i = 0; i < accountBookid.Length; i++ )
            {
                PrivyAccountBook book = GetPrivyAccountBook( TollCollectorId, accountBookid[i], tbInvoiceList, tbInvoiceDetailList ,tbAccountList );
                lstBooks.Add( book );
            }
            return lstBooks;
        }
        /// <summary>
        /// 获取缴款单数据
        /// </summary>
        /// <param name="TollCollectorId"></param>
        /// <param name="AccountBookId"></param>
        public static void GetAccountData( int TollCollectorId, int[] AccountBookIds,
                                                out DataTable tbInvoiceList ,out DataTable tbInvoiceDetailList )
        {
            tbInvoiceList = null;
            tbInvoiceDetailList = null;
            string accountList = "";
            for ( int i = 0 ; i < AccountBookIds.Length -1 ; i++ )
                accountList += AccountBookIds[i].ToString( ) + ",";
            accountList += AccountBookIds[AccountBookIds.Length - 1].ToString( );

            //发票列表(所有的发票，包含挂号和收费)
            string sql = @"select a.patlistid,b.meditype,a.record_flag, ticketnum,total_fee,village_fee,favor_fee,pos_fee,self_tally, 
                                    (total_fee-village_fee-favor_fee-pos_fee-self_tally) as money_fee ,a.hang_flag,a.costdate,a.accountid
                            from mz_costmaster a,mz_patlist b 
                            where a.patlistid=b.patlistid 
                            and a.record_flag in (0,1,2)
                            and a.chargecode='" + TollCollectorId.ToString( ) + @"'
                            and a.accountid in ( " + accountList + @")
                            and a.workid=" + WORK_ID + @"
                            and b.workid= " + WORK_ID + @"
                             order by costdate";
            tbInvoiceList = oleDb.GetDataTable( sql );
            //发票明细列表(所有的，包含挂号和收费)
            sql = @"select a.ticketnum,c.code as stat_item_code,c.mzfp_code,d.item_name, b.total_fee ,a.record_flag,a.hang_flag,
                        a.village_fee,a.favor_fee,a.pos_fee,a.self_tally, 
                        (a.total_fee-a.village_fee-a.favor_fee-a.pos_fee-a.self_tally) as money_fee,a.accountid
                    from mz_costmaster a,mz_costorder b ,base_stat_item c,base_stat_mzfp d 
                    where a.costmasterid = b.costid 
                    and b.itemtype=c.code
                    and c.mzfp_code = d.code 
                    and a.record_flag in (0,1,2)
                    and a.chargecode='" + TollCollectorId.ToString( ) + @"'
                    and a.accountid in (" + accountList + @") 
                    and a.workid=" + WORK_ID + @"
                    and b.workid= " + WORK_ID;
            tbInvoiceDetailList = oleDb.GetDataTable( sql );

        }
        /// <summary>
        /// 交款
        /// </summary>
        /// <param name="userId">操作员ID（EmployeeId）</param>
        public static bool HandInAccount( int userId ,PrivyAccountBook accountBook)
        {

            string strblankoutInvoices = "";
            foreach ( string invoiceNum in accountBook.ChargeInvoiceInfo.Useless )
                strblankoutInvoices = strblankoutInvoices + invoiceNum + "|";
            if ( strblankoutInvoices.Trim( ) != "" )
                strblankoutInvoices = strblankoutInvoices.Remove( strblankoutInvoices.Length - 1 , 1 );

            oleDb.BeginTransaction( );

            try
            {

                HIS.Model.MZ_Account new_mz_account = new HIS.Model.MZ_Account( );
                new_mz_account.AccountCode = userId.ToString( );
                new_mz_account.AccountDate = HIS.SYSTEM.PubicBaseClasses.XcDate.ServerDateTime;
                new_mz_account.Total_Fee = accountBook.InvoiceItemSumTotal;
                new_mz_account.Cash_Fee = 0;
                new_mz_account.POS_Fee = 0;
                new_mz_account.BlankOut = strblankoutInvoices;
                //新增账单
                HIS.SYSTEM.Core.BindEntity<HIS.Model.MZ_Account>.CreateInstanceDAL( oleDb ).Add( new_mz_account );
                //当前对象ID赋值
                int accountBookId = new_mz_account.AccountID;
                //更新结算表，置账单号
                List<HIS.Model.MZ_CostMaster> list_costmaster = HIS.SYSTEM.Core.BindEntity<HIS.Model.MZ_CostMaster>.CreateInstanceDAL( oleDb ).GetListArray( " RECORD_FLAG<>9 and ACCOUNTID=0 and  CHARGECODE='" + userId + "'" );
                if ( list_costmaster.Count == 0 )
                    throw new OperatorException( "没有科目可以交账！" );
                for ( int i = 0 ; i < list_costmaster.Count ; i++ )
                {
                    list_costmaster[i].AccountID = accountBookId;
                    HIS.SYSTEM.Core.BindEntity<HIS.Model.MZ_CostMaster>.CreateInstanceDAL( oleDb ).Update( list_costmaster[i] );
                }
                oleDb.CommitTransaction( );
                return true;
            }
            catch ( OperatorException operr )
            {
                oleDb.RollbackTransaction( );
                throw operr;
            }
            catch ( Exception err )
            {
                oleDb.RollbackTransaction( );
                ErrorWriter.WriteLog( err.Message );
                throw new Exception( "交款处理发生错误！" );
            }

        }
        /// <summary>
        /// 获取交款记录
        /// </summary>
        /// <param name="OperatorID">交款人ID</param>
        /// <returns>ACCOUNTID,ACCOUNTDATE</returns>
        public static DataTable GetAccountHandInDate( int OperatorID )
        {
            HIS.DAL.MZ_DAL mz_dal = new HIS.DAL.MZ_DAL( );
            mz_dal._OleDB = oleDb;
            return mz_dal.GetAccountHandInDate( OperatorID );

        }
        /// <summary>
        /// 获取指定时间段内的交账记录
        /// </summary>
        /// <param name="beginDate">开始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns></returns>
        public static DataTable GetAccountList( DateTime beginDate , DateTime endDate )
        {
            HIS.DAL.MZ_DAL mz_dal = new HIS.DAL.MZ_DAL( );
            mz_dal._OleDB = oleDb;
            return mz_dal.GetAccountList( beginDate , endDate );
        }
        /// <summary>
        /// 获取没有交账的人员列表
        /// </summary>
        /// <returns></returns>
        public static DataTable GetNotHandInAccountUser()
        {
            HIS.DAL.MZ_DAL mz_dal = new HIS.DAL.MZ_DAL( );
            mz_dal._OleDB = oleDb;
            return mz_dal.GetNotHandInAccountChargeor( );
        }
        /// <summary>
        /// 汇总个人账单
        /// </summary>
        /// <param name="Books"></param>
        /// <returns></returns>
        public static PrivyAccountBook CollectPrivyAccountBook( List<PrivyAccountBook> Books )
        {
            PrivyAccountBook totalBook = new PrivyAccountBook( );
            AccountBillInfo chargeBillinfo = new AccountBillInfo( );
            AccountBillInfo registerBillinfo = new AccountBillInfo( );
            FundPart cashPart = new FundPart( );
            FundPart favorPart = new FundPart( );
            FundPart tallyPart = new FundPart( );
            Hashtable htInvoiceItem = new Hashtable( );

            foreach ( PrivyAccountBook book in Books )
            {
                totalBook.InvoiceItemSumTotal += book.InvoiceItemSumTotal;

                #region 合并收费票据部分
                chargeBillinfo.Count += book.ChargeInvoiceInfo.Count;
                if ( ( chargeBillinfo.StartNumber == "" || chargeBillinfo.StartNumber == null ) && book.ChargeInvoiceInfo.StartNumber != "" )
                    chargeBillinfo.StartNumber = book.ChargeInvoiceInfo.StartNumber;
                if ( book.ChargeInvoiceInfo.EndNumber != "" )
                    chargeBillinfo.EndNumber = book.ChargeInvoiceInfo.EndNumber;

                chargeBillinfo.RefundCount += book.ChargeInvoiceInfo.RefundCount;
                chargeBillinfo.RefundMoney += book.ChargeInvoiceInfo.RefundMoney;
                chargeBillinfo.ChargeName = book.ChargeInvoiceInfo.ChargeName;
                List<Invoice> lstChargeInvoice = new List<Invoice>( );
                if ( chargeBillinfo.InvoiceList != null )
                    lstChargeInvoice = chargeBillinfo.InvoiceList.ToList( );
                if ( book.ChargeInvoiceInfo.InvoiceList != null )
                {
                    for ( int i = 0 ; i < book.ChargeInvoiceInfo.InvoiceList.Length ; i++ )
                        lstChargeInvoice.Add( book.ChargeInvoiceInfo.InvoiceList[i] );
                }
                chargeBillinfo.InvoiceList = lstChargeInvoice.ToArray( );
                //退票
                List<Invoice> lstChargeRefundInvoice = new List<Invoice>( );
                if ( chargeBillinfo.RefundInvoice != null )
                    lstChargeRefundInvoice = chargeBillinfo.RefundInvoice.ToList( );
                if ( book.ChargeInvoiceInfo.RefundInvoice != null )
                {
                    for ( int i = 0 ; i < book.ChargeInvoiceInfo.RefundInvoice.Length ; i++ )
                        lstChargeRefundInvoice.Add( book.ChargeInvoiceInfo.RefundInvoice[i] );
                }
                chargeBillinfo.RefundInvoice = lstChargeRefundInvoice.ToArray( );
                #endregion

                #region 合并挂号票据部分
                registerBillinfo.Count += book.RegisterInvoiceInfo.Count;
                if ( ( registerBillinfo.StartNumber == "" || registerBillinfo.StartNumber == null ) && book.RegisterInvoiceInfo.StartNumber != "" )
                    registerBillinfo.StartNumber = book.RegisterInvoiceInfo.StartNumber;
                if ( book.RegisterInvoiceInfo.EndNumber != "" )
                    registerBillinfo.EndNumber = book.RegisterInvoiceInfo.EndNumber;

                registerBillinfo.RefundCount += book.RegisterInvoiceInfo.RefundCount;
                registerBillinfo.RefundMoney += book.RegisterInvoiceInfo.RefundMoney;
                registerBillinfo.ChargeName = book.RegisterInvoiceInfo.ChargeName;
                List<Invoice> lstRegInvoice = new List<Invoice>( );
                if ( registerBillinfo.InvoiceList != null )
                    lstRegInvoice = registerBillinfo.InvoiceList.ToList( );
                if ( book.RegisterInvoiceInfo.InvoiceList != null )
                {
                    for ( int i = 0 ; i < book.RegisterInvoiceInfo.InvoiceList.Length ; i++ )
                        lstRegInvoice.Add( book.RegisterInvoiceInfo.InvoiceList[i] );
                }
                registerBillinfo.InvoiceList = lstRegInvoice.ToArray( );

                List<Invoice> lstRegRefundInvoice = new List<Invoice>( );
                if ( registerBillinfo.RefundInvoice != null )
                    lstRegRefundInvoice = registerBillinfo.RefundInvoice.ToList( );
                if ( book.RegisterInvoiceInfo.RefundInvoice != null )
                {
                    for ( int i = 0 ; i < book.RegisterInvoiceInfo.RefundInvoice.Length ; i++ )
                        lstRegRefundInvoice.Add( book.RegisterInvoiceInfo.RefundInvoice[i] );
                }
                registerBillinfo.RefundInvoice = lstRegRefundInvoice.ToArray( );
                #endregion

                #region 合并发票科目部分
                for ( int i = 0 ; i < book.InvoiceItem.Length ; i++ )
                {
                    if ( htInvoiceItem.ContainsKey( book.InvoiceItem[i].ItemCode ) )
                    {
                        InvoiceItem invoiceItem = (InvoiceItem)htInvoiceItem[book.InvoiceItem[i].ItemCode];
                        invoiceItem.Cost += book.InvoiceItem[i].Cost;
                        htInvoiceItem.Remove( book.InvoiceItem[i].ItemCode );
                        htInvoiceItem.Add( invoiceItem.ItemCode , invoiceItem );
                    }
                    else
                    {
                        htInvoiceItem.Add( book.InvoiceItem[i].ItemCode , book.InvoiceItem[i] );
                    }
                }
                #endregion

                #region 合并现金部分
                cashPart.TotalMoney += book.CashPart.TotalMoney;
                if ( cashPart.Details == null )
                    cashPart.Details = new FundInfo[3];
                for ( int i = 0 ; i < book.CashPart.Details.Length ; i++ )
                {
                    cashPart.Details[i].BillCount += book.CashPart.Details[i].BillCount;
                    cashPart.Details[i].Money += book.CashPart.Details[i].Money;
                    cashPart.Details[i].PayCode = book.CashPart.Details[i].PayCode;
                    cashPart.Details[i].PayName = book.CashPart.Details[i].PayName;
                }
                #endregion

                #region 合并记账部分
                tallyPart.TotalMoney += book.TallyPart.TotalMoney;
                if ( tallyPart.Details == null )
                    tallyPart.Details = new FundInfo[book.TallyPart.Details.Length];

                for ( int i = 0 ; i < book.TallyPart.Details.Length ; i++ )
                {
                    tallyPart.Details[i].BillCount += book.TallyPart.Details[i].BillCount;
                    tallyPart.Details[i].Money += book.TallyPart.Details[i].Money;
                    tallyPart.Details[i].PayCode = book.TallyPart.Details[i].PayCode;
                    tallyPart.Details[i].PayName = book.TallyPart.Details[i].PayName;
                }
                #endregion

                //合并优惠部分
                favorPart.TotalMoney += book.FavorPart.TotalMoney;
            }

            totalBook.ChargeInvoiceInfo = chargeBillinfo;
            totalBook.RegisterInvoiceInfo = registerBillinfo;
            totalBook.CashPart = cashPart;
            totalBook.TallyPart = tallyPart;
            totalBook.FavorPart = favorPart;

            totalBook.InvoiceItem = new InvoiceItem[htInvoiceItem.Count];
            int count = 0;
            foreach ( object obj in htInvoiceItem )
            {
                totalBook.InvoiceItem[count] = (InvoiceItem)( (DictionaryEntry)obj ).Value;
                count++;
            }

            return totalBook;
        }
        /// <summary>
        /// 汇总所有人账单
        /// </summary>
        /// <param name="Books"></param>
        /// <returns></returns>
        public static CollectAccountBook CollectAllAccountBook( List<PrivyAccountBook> Books )
        {
            CollectAccountBook totalBook = new CollectAccountBook( );
            List<AccountBillInfo> chargeBillinfo = new List<AccountBillInfo>( );
            List<AccountBillInfo> registerBillinfo = new List<AccountBillInfo>( );
            FundPart cashPart = new FundPart( );
            FundPart favorPart = new FundPart( );
            FundPart tallyPart = new FundPart( );
            Hashtable htInvoiceItem = new Hashtable( );

            foreach ( PrivyAccountBook book in Books )
            {
                #region 合并发票科目部分
                if (book == null || book.InvoiceItem == null)
                {
                    continue;
                }
                totalBook.InvoiceItemSumTotal += book.InvoiceItemSumTotal;

                #region 合并收费票据部分
                chargeBillinfo.Add( book.ChargeInvoiceInfo );
                //chargeBillinfo.Count += book.ChargeInvoiceInfo.Count;

                //chargeBillinfo.RefundCount += book.ChargeInvoiceInfo.RefundCount;
                //chargeBillinfo.RefundMoney += book.ChargeInvoiceInfo.RefundMoney;
                //List<Invoice> lstChargeInvoice = new List<Invoice>( );
                //if ( chargeBillinfo.InvoiceList != null )
                //    lstChargeInvoice = chargeBillinfo.InvoiceList.ToList( );
                //if ( book.ChargeInvoiceInfo.InvoiceList != null )
                //{
                //    for ( int i = 0 ; i < book.ChargeInvoiceInfo.InvoiceList.Length ; i++ )
                //        lstChargeInvoice.Add( book.ChargeInvoiceInfo.InvoiceList[i] );
                //}
                //chargeBillinfo.InvoiceList = lstChargeInvoice.ToArray( );
                #endregion

                #region 合并挂号票据部分
                //registerBillinfo.Count += book.RegisterInvoiceInfo.Count;
                ////if ( ( registerBillinfo.StartNumber == "" || registerBillinfo.StartNumber == null ) && book.RegisterInvoiceInfo.StartNumber != "" )
                ////    registerBillinfo.StartNumber = book.RegisterInvoiceInfo.StartNumber;
                ////if ( book.RegisterInvoiceInfo.EndNumber != "" )
                ////    registerBillinfo.EndNumber = book.RegisterInvoiceInfo.EndNumber;

                //registerBillinfo.RefundCount += book.RegisterInvoiceInfo.RefundCount;
                //registerBillinfo.RefundMoney += book.RegisterInvoiceInfo.RefundMoney;
                //List<Invoice> lstRegInvoice = new List<Invoice>( );
                //if ( registerBillinfo.InvoiceList != null )
                //    lstRegInvoice = registerBillinfo.InvoiceList.ToList( );
                //if ( book.RegisterInvoiceInfo.InvoiceList != null )
                //{
                //    for ( int i = 0 ; i < book.RegisterInvoiceInfo.InvoiceList.Length ; i++ )
                //        lstRegInvoice.Add( book.RegisterInvoiceInfo.InvoiceList[i] );
                //}
                //registerBillinfo.InvoiceList = lstRegInvoice.ToArray( );
                registerBillinfo.Add( book.RegisterInvoiceInfo );
                #endregion

               
                for ( int i = 0 ; i < book.InvoiceItem.Length ; i++ )
                {
                    if ( htInvoiceItem.ContainsKey( book.InvoiceItem[i].ItemCode ) )
                    {
                        InvoiceItem invoiceItem = (InvoiceItem)htInvoiceItem[book.InvoiceItem[i].ItemCode];
                        invoiceItem.Cost += book.InvoiceItem[i].Cost;
                        htInvoiceItem.Remove( book.InvoiceItem[i].ItemCode );
                        htInvoiceItem.Add( invoiceItem.ItemCode , invoiceItem );
                    }
                    else
                    {
                        htInvoiceItem.Add( book.InvoiceItem[i].ItemCode , book.InvoiceItem[i] );
                    }
                }
                #endregion

                #region 合并现金部分
                cashPart.TotalMoney += book.CashPart.TotalMoney;
                if ( cashPart.Details == null )
                    cashPart.Details = new FundInfo[3];
                for ( int i = 0 ; i < book.CashPart.Details.Length ; i++ )
                {
                    cashPart.Details[i].BillCount += book.CashPart.Details[i].BillCount;
                    cashPart.Details[i].Money += book.CashPart.Details[i].Money;
                    cashPart.Details[i].PayCode = book.CashPart.Details[i].PayCode;
                    cashPart.Details[i].PayName = book.CashPart.Details[i].PayName;
                }
                #endregion

                #region 合并记账部分
                tallyPart.TotalMoney += book.TallyPart.TotalMoney;
                if ( tallyPart.Details == null )
                    tallyPart.Details = new FundInfo[book.TallyPart.Details.Length];

                for ( int i = 0 ; i < book.TallyPart.Details.Length ; i++ )
                {
                    tallyPart.Details[i].BillCount += book.TallyPart.Details[i].BillCount;
                    tallyPart.Details[i].Money += book.TallyPart.Details[i].Money;
                    tallyPart.Details[i].PayCode = book.TallyPart.Details[i].PayCode;
                    tallyPart.Details[i].PayName = book.TallyPart.Details[i].PayName;
                }
                #endregion

                //合并优惠部分
                favorPart.TotalMoney += book.FavorPart.TotalMoney;
            }

            totalBook.ChargeInvoiceInfo = chargeBillinfo.ToArray();
            totalBook.RegisterInvoiceInfo = registerBillinfo.ToArray();
            totalBook.CashPart = cashPart;
            totalBook.TallyPart = tallyPart;
            totalBook.FavorPart = favorPart;

            totalBook.InvoiceItem = new InvoiceItem[htInvoiceItem.Count];
            int count = 0;
            foreach ( object obj in htInvoiceItem )
            {
                totalBook.InvoiceItem[count] = (InvoiceItem)( (DictionaryEntry)obj ).Value;
                count++;
            }

            return totalBook;
        }
        /// <summary>
        /// 得到票据信息
        /// </summary>
        /// <param name="kind">票据类型</param>
        /// <param name="tbAllInvoiceList">所有的发票列表</param>
        /// <returns>根据票据类型返回的票据信息</returns>
        private static AccountBillInfo GetBillInfo( OPDOperationType kind , DataTable tbAllInvoiceList )
        {
            int flag = (int)kind;
            DataTable tbInvoice = tbAllInvoiceList.Clone( );
            DataRow[] drsInvoice = tbAllInvoiceList.Select( "hang_flag=" + flag , "costdate asc" );
            for ( int i = 0 ; i < drsInvoice.Length ; i++ )
                tbInvoice.Rows.Add( drsInvoice[i].ItemArray );

            AccountBillInfo billInfo = new AccountBillInfo( );
            if ( drsInvoice.Length > 0 )
            {
                DataRow[] drs = tbInvoice.Select( "record_flag in (0,1)","ticketnum asc" );
                billInfo.Count = drs.Length;
                if ( drs.Length > 0 )
                {
                    billInfo.StartNumber = drs[0]["ticketnum"].ToString( ).Trim( );
                    billInfo.EndNumber = drs[drs.Length - 1]["ticketnum"].ToString( ).Trim( );
                }
                //构造发票清单
                billInfo.InvoiceList = new Invoice[drs.Length];
                for ( int i = 0 ; i < drs.Length ; i++ )
                {
                    billInfo.InvoiceList[i] = new Invoice( );
                    billInfo.InvoiceList[i].InvoiceNo = drs[i]["ticketnum"].ToString( ).Trim( );
                    //billInfo.InvoiceList[i].PatientName = PublicDataReader.GetPatientTypeNameByCode( drs[i]["meditype"].ToString( ).Trim( ) );
                    billInfo.InvoiceList[i].PatientName = BaseDataController.GetName( BaseDataCatalog.病人类型列表, drs[i]["meditype"].ToString().Trim() );
                    billInfo.InvoiceList[i].TotalPay = Convert.ToDecimal( drs[i]["total_fee"] );
                    billInfo.InvoiceList[i].CashPay = Convert.ToDecimal( drs[i]["money_fee"] );
                    billInfo.InvoiceList[i].PosPay = Convert.ToDecimal( drs[i]["pos_fee"] );
                    billInfo.InvoiceList[i].VillagePay = Convert.ToDecimal( drs[i]["village_fee"] );
                    billInfo.InvoiceList[i].SelfTally = Convert.ToDecimal( drs[i]["self_tally"] );
                    billInfo.InvoiceList[i].FavorPay = Convert.ToDecimal( drs[i]["favor_fee"] );
                    billInfo.InvoiceList[i].ChargeDate = Convert.ToDateTime( drs[i]["costdate"] );
                    billInfo.InvoiceList[i].RecordFlag = Convert.ToInt32( drs[i]["record_flag"] );
                }
                //退票
                DataRow[] drsRefund = tbInvoice.Select( "record_flag = 2 " );
                billInfo.RefundCount = drsRefund.Length;
                billInfo.RefundInvoice = new Invoice[billInfo.RefundCount];
                for ( int i = 0 ; i < drsRefund.Length ; i++ )
                {
                    billInfo.RefundMoney += Math.Abs( Convert.ToDecimal( drsRefund[i]["total_fee"] ) );

                    billInfo.RefundInvoice[i] = new Invoice( );
                    billInfo.RefundInvoice[i].InvoiceNo = drsRefund[i]["ticketnum"].ToString( ).Trim( );
                    //billInfo.RefundInvoice[i].PatientName = PublicDataReader.GetPatientTypeNameByCode( drsRefund[i]["meditype"].ToString( ).Trim( ) );
                    billInfo.RefundInvoice[i].PatientName = BaseDataController.GetName( BaseDataCatalog.病人类型列表, drsRefund[i]["meditype"].ToString().Trim() );
                    billInfo.RefundInvoice[i].TotalPay = Math.Abs( Convert.ToDecimal( drsRefund[i]["total_fee"] ) );
                    billInfo.RefundInvoice[i].CashPay = Math.Abs( Convert.ToDecimal( drsRefund[i]["money_fee"] ) );
                    billInfo.RefundInvoice[i].PosPay = Math.Abs( Convert.ToDecimal( drsRefund[i]["pos_fee"] ) );
                    billInfo.RefundInvoice[i].VillagePay =Math.Abs( Convert.ToDecimal( drsRefund[i]["village_fee"] ) );
                    billInfo.RefundInvoice[i].SelfTally = Math.Abs( Convert.ToDecimal( drsRefund[i]["self_tally"] ) );
                    billInfo.RefundInvoice[i].FavorPay = Math.Abs( Convert.ToDecimal( drsRefund[i]["favor_fee"] ) );
                    billInfo.RefundInvoice[i].ChargeDate = Convert.ToDateTime( drsRefund[i]["costdate"] );
                    billInfo.RefundInvoice[i].RecordFlag = Convert.ToInt32( drsRefund[i]["record_flag"] );
                }
                billInfo.Useless = new string[] { "" };
            }
            else
            {
                billInfo.Count = 0;
                billInfo.StartNumber = "";
                billInfo.EndNumber = "";
                billInfo.RefundMoney = 0;
                billInfo.RefundCount = 0;
                billInfo.Useless = new string[] { "" };
            }
            return billInfo;
        }
        /// <summary>
        /// 获取优惠部分
        /// </summary>
        /// <param name="tbAllInvoiceList"></param>
        /// <returns></returns>
        private static FundPart GetFavorPart( DataTable tbAllInvoiceList )
        {
            FundPart favorPart = new FundPart( );
            object objSum = tbAllInvoiceList.Compute( "Sum(favor_fee)" , "" );
            favorPart.TotalMoney = Convert.IsDBNull( objSum ) ? 0 : Convert.ToDecimal( objSum );

            return favorPart;
        }
        /// <summary>
        /// 获取实收现金部分
        /// </summary>
        /// <param name="tbAllInvoiceList"></param>
        /// <returns></returns>
        private static FundPart GetCashPart( DataTable tbAllInvoiceList , DataTable tbAllInvoiceDetailList )
        {
            FundPart cashPart = new FundPart( );
            object objSum = null;
            //实收现金
            objSum = tbAllInvoiceList.Compute( "SUM(money_fee)" , "" );
            cashPart.TotalMoney = Convert.IsDBNull( objSum ) ? 0 : Convert.ToDecimal( objSum );

            DataTable tbChargeInvoiceDetaiList = tbAllInvoiceDetailList.Clone( );
            DataTable tbRegisterInvoiceDetaiList = tbAllInvoiceDetailList.Clone( );
            //分开挂号和收费的明细
            for ( int i = 0 ; i < tbAllInvoiceDetailList.Rows.Count ; i++ )
            {
                int flag = Convert.ToInt32( tbAllInvoiceDetailList.Rows[i]["hang_flag"] );
                if ( flag == 0 )
                    tbRegisterInvoiceDetaiList.Rows.Add( tbAllInvoiceDetailList.Rows[i].ItemArray );
                else
                    tbChargeInvoiceDetaiList.Rows.Add( tbAllInvoiceDetailList.Rows[i].ItemArray );
            }
            //处方收费
            FundInfo fundCharge = new FundInfo( );
            fundCharge.PayName = "处方收费";
            objSum = tbAllInvoiceList.Compute( "Sum(money_fee)" , "hang_flag = 1" );
            fundCharge.Money = Convert.IsDBNull( objSum ) ? 0 : Convert.ToDecimal( objSum );
            //挂号收费
            FundInfo fundRegFee = new FundInfo( );
            fundRegFee.PayName = "挂号费";
            objSum = tbRegisterInvoiceDetaiList.Compute( "sum(total_fee)" , "stat_item_code='05' and hang_flag=0 and money_fee<>0" );
            fundRegFee.Money = Convert.IsDBNull( objSum ) ? 0 : Convert.ToDecimal( objSum );
            //挂号诊金
            FundInfo fundExamine = new FundInfo( );
            fundExamine.PayName = "诊查费";
            objSum = tbRegisterInvoiceDetaiList.Compute( "sum(total_fee)" , "stat_item_code='06' and hang_flag=0 and money_fee<>0" );
            fundExamine.Money = Convert.IsDBNull( objSum ) ? 0 : Convert.ToDecimal( objSum );

            cashPart.Details = new FundInfo[3];
            cashPart.Details[0] = fundCharge;
            cashPart.Details[1] = fundRegFee;
            cashPart.Details[2] = fundExamine;

            return cashPart;
        }
        /// <summary>
        /// 获取记账部分
        /// </summary>
        /// <param name="tbAllInvoiceList"></param>
        /// <returns></returns>
        private static FundPart GetTallyPart( DataTable tbAllInvoiceList )
        {
            List<FundInfo> lstFundInfo = new List<FundInfo>( );
            decimal totalTally = 0;
            //按病人类型获取记账
            DataTable PatientTypeList = BaseDataController.BaseDataSet[BaseDataCatalog.病人类型列表];
            FundInfo[] fundPatType = new FundInfo[PatientTypeList.Rows.Count];
            for ( int i = 0 ; i < PatientTypeList.Rows.Count ; i++ )
            {
                fundPatType[i].PayCode = PatientTypeList.Rows[i]["PATTYPECODE"].ToString( ).Trim( );
                fundPatType[i].PayName = PatientTypeList.Rows[i]["NAME"].ToString( ).Trim( ) + "_记账";
            }
            for ( int i = 0 ; i < fundPatType.Length ; i++ )
            {
                DataRow[] drsInvoice = tbAllInvoiceList.Select( "meditype='" + fundPatType[i].PayCode + "'" );
                for ( int j = 0 ; j < drsInvoice.Length ; j++ )
                {
                    decimal tallyMoney = Convert.ToDecimal( drsInvoice[j]["village_fee"] );
                    if ( tallyMoney != 0 )
                    {
                        fundPatType[i].BillCount = fundPatType[i].BillCount + 1;
                        fundPatType[i].Money = fundPatType[i].Money + tallyMoney;
                        totalTally = totalTally + tallyMoney;
                    }
                }
            }
            lstFundInfo = fundPatType.ToList( );

            //POS记账
            FundInfo fundPos = new FundInfo( );
            fundPos.BillCount = 0;
            fundPos.PayName = "POS金额";
            //个人记账
            FundInfo fundSelfTally = new FundInfo( );
            fundSelfTally.PayName = "单位记账";

            for ( int i = 0 ; i < tbAllInvoiceList.Rows.Count ; i++ )
            {
                decimal posMoney = Convert.ToDecimal( tbAllInvoiceList.Rows[i]["pos_fee"] );
                decimal selfTallyMoney = Convert.ToDecimal( tbAllInvoiceList.Rows[i]["self_tally"] );
                if ( posMoney != 0 )
                {
                    fundPos.BillCount = fundPos.BillCount + 1;
                    fundPos.Money = fundPos.Money + posMoney;
                    totalTally = totalTally + posMoney;
                }
                if ( selfTallyMoney != 0 )
                {
                    fundSelfTally.BillCount = fundSelfTally.BillCount + 1;
                    fundSelfTally.Money = fundSelfTally.Money + selfTallyMoney;
                    totalTally = totalTally + selfTallyMoney;
                }
            }
            lstFundInfo.Add( fundPos );
            lstFundInfo.Add( fundSelfTally );

            FundPart tallyPart = new FundPart( );
            tallyPart.TotalMoney = totalTally;
            tallyPart.Details = lstFundInfo.ToArray( );

            return tallyPart;
        }
        /// <summary>
        /// 获取账单的统计大项目列表
        /// </summary>
        /// <param name="AccountId"></param>
        /// <param name="TollerId"></param>
        /// <returns></returns>
        public static DataTable GetAccountStatItemList( int AccountId , int TollerId )
        {
            string workId = HIS.SYSTEM.Core.EntityConfig.WorkID.ToString( );

//            string sql = @"select deptcode as ID,bigitemcode as code,sum(sub_item_fee) as total_fee  
//                            from (select deptcode,bigitemcode,sub_item_fee
//	                              from  (select a.costmasterid, b.itemtype as bigitemcode, b.total_fee as sub_item_fee
//		  	                             from  
//                                         (select * from mz_costmaster where workid=%workid and chargecode='%tollerId') a, 
//		                                 (select * from mz_costorder where workid=%workid ) b, 
//		                                 base_stat_item c 
//		                                 where a.costmasterid=b.costid and b.itemtype=c.code and record_flag in (0,1,2) 
//		                                 and a.costdate between '%date1' and '%date2' and a.accountid<>0 and a.accountid = %accountId 
//		                            ) a1, 
//		                            ( select distinct costmasterid, 
//				                             (select presdeptcode from mz_presmaster b where b.workid=%workid and a.costmasterid = b.costmasterid and b.record_flag in (0,1,2) order by presdeptcode fetch first 1 rows only) deptcode 
//		                            from mz_presmaster a  
//		                            where workid=%workid 
//		                            and costmasterid in (select costmasterid 
//						 	                             from mz_costmaster where workid=%workid and chargecode='%tollerId'
//							                             and costdate between '%date1' and '%date2' and record_flag in (0,1,2)) 
//		                            ) a2  
//                            where a1.costmasterid = a2.costmasterid 
//                            ) a  group by deptcode,bigitemcode";

            string sql = @"select deptcode as ID,bigitemcode as code,sum(sub_item_fee) as total_fee  
                            from (select deptcode,bigitemcode,sub_item_fee
	                              from  (select a.costmasterid, b.itemtype as bigitemcode, b.total_fee as sub_item_fee
		  	                             from  
                                         (select * from mz_costmaster where workid=%workid and chargecode='%tollerId') a, 
		                                 (select * from mz_costorder where workid=%workid ) b, 
		                                 base_stat_item c 
		                                 where a.costmasterid=b.costid and b.itemtype=c.code and record_flag in (0,1,2) 
		                                 and a.accountid<>0 and a.accountid = %accountId 
		                            ) a1, 
		                            ( select distinct costmasterid, 
				                             (select presdeptcode from mz_presmaster b where b.workid=%workid and a.costmasterid = b.costmasterid and b.record_flag in (0,1,2) order by presdeptcode fetch first 1 rows only) deptcode 
		                            from mz_presmaster a  
		                            where workid=%workid 
		                            and costmasterid in (select costmasterid 
						 	                             from mz_costmaster where workid=%workid and chargecode='%tollerId'
							                             and  record_flag in (0,1,2)) 
		                            ) a2  
                            where a1.costmasterid = a2.costmasterid 
                            ) a  group by deptcode,bigitemcode";

            sql = sql.Replace( "%workid" , workId );
            //sql = sql.Replace( "%date1" , dateFrom.ToString( "yyyy-MM-dd HH:mm:ss" ) );
            //sql = sql.Replace( "%date2" , dateEnd.ToString( "yyyy-MM-dd HH:mm:ss" ) );
            sql = sql.Replace( "%tollerId" , TollerId.ToString( ) );
            sql = sql.Replace( "%accountId" , AccountId.ToString( ) );

            return oleDb.GetDataTable( sql );
        }
    }
}
