using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using B2BTask.Controllers;
using Com.ACBC.Framework.Database;
using Pomelo.AspNetCore.TimedJob;

namespace core测试.Controllers
{
    public class BBCOrderJob : Job
    {
        // Begin 起始时间；Interval执行时间间隔，单位是毫秒，建议使用以下格式，此处为半小时；SkipWhileExecuting是否等待上一个执行完成，true为等待；
        [Invoke(Begin = "2016-11-29 22:10", Interval = 1000, SkipWhileExecuting = true)]
        public void Run()
        {
            BBCDBManager bbc = new BBCDBManager();
            DatabaseOperationWeb.TYPE = bbc;
            //处理bbc-->b2b
            string sql = "select  cast(o.paytype as SIGNED INTEGER) paytype1,m.id as consigneeCode,o.* " +
                "from ims_ewei_shop_order o ,(select min(id) id,openid from ims_ewei_shop_member group by openid) m  " +
                "where o.openid = m.openid " +
                "and  (o.virtual_info is null or (o.virtual_info !=o.`status` and  o.virtual_info !='-1')) " +
                "and o.`status` >0 and o.id > 333";
            DataTable dt = DatabaseOperationWeb.ExecuteSelectDS(sql, "ims_ewei_shop_order").Tables[0];
            if (dt.Rows.Count > 0)
            {
                BBCTOB2B(dt);
                //MessageBox.Show(err);
            }

            //处理b2b-->bbc
            //B2BDBManager b2b = new B2BDBManager();
            //DatabaseOperationWeb.TYPE = b2b;
            //string sql1 = "select * from t_order_list where toBBC='1' and status='0' ";
            //DataTable dt1 = DatabaseOperationWeb.ExecuteSelectDS(sql1, "ims_ewei_shop_order").Tables[0];
            //if (dt1.Rows.Count > 0)
            //{
            //    B2BTOBBC(dt1);
            //    //MessageBox.Show(err);
            //}
        }

        private void BBCTOB2B(DataTable dt)
        {
            BBCDBManager bbc = new BBCDBManager();
            B2BDBManager b2b = new B2BDBManager();
            ArrayList b2bAL = new ArrayList();
            ArrayList bbcAL = new ArrayList();

            DatabaseOperationWeb.TYPE = b2b;

            #region 生成订单list
            List<OrderItem> OrderItemList = new List<OrderItem>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows[i]["status"].ToString() == "1")//付款订单等待发货
                {
                    if (dt.Rows[i]["virtual_info"].ToString() == "")//未处理过
                    {
                        string purchaserCode = "", distributionCode = "";
                        string userSql = "select id, usercode from t_user_list where uniacid ='" + dt.Rows[i]["uniacid"].ToString() + "'";
                        DataTable userDt = DatabaseOperationWeb.ExecuteSelectDS(userSql, "t_user_list").Tables[0];
                        if (userDt.Rows.Count > 0)//如果在b2b中有代理编号的才做处理
                        {
                            OrderItem orderItem = new OrderItem();
                            purchaserCode = userDt.Rows[0]["usercode"].ToString();
                            //if (dt.Rows[i]["agentid"].ToString() != "0")//判断是否有分销商
                            //{
                            //    string userSql1 = "select usercode from t_user_list where agentid = '" + dt.Rows[i]["agentid"].ToString() + "'";
                            //    DataTable userDt1 = DatabaseOperationWeb.ExecuteSelectDS(userSql1, "ims_ewei_shop_order").Tables[0];
                            //    if (userDt1.Rows.Count > 0)
                            //    {
                            //        distributionCode = userDt.Rows[0][0].ToString();
                            //    }
                            //    else
                            //    {
                            //        //没有对应分销商，先不处理
                            //    }
                            //}
                            if (dt.Rows[i]["openid"].ToString().IndexOf("sns_wa_")>=0)//判断是否有openid
                            {
                                string openid = dt.Rows[i]["openid"].ToString().Replace("sns_wa_", "");
                                string dsql = "select u.* from t_wxapp_pagent_member m,t_user_list u " +
                                    "where m.supplierCode= u.usercode and u.usertype='4' " +
                                    "and m.openId= 'oQXDM4sVG6fS-Jdu3tnnuDFDo-Xc' " +
                                    "and m.purchasersCode= 'bbcagent@llwell.net'";
                                DataTable dDt = DatabaseOperationWeb.ExecuteSelectDS(dsql, "t_user_list").Tables[0];
                                if (dDt.Rows.Count>0)
                                {
                                    distributionCode = dDt.Rows[0]["usercode"].ToString();
                                }
                            }
                            string[] address = dt.Rows[i]["address"].ToString().Split(';');
                            string name = (address[7].Replace("\"", "").Split(':'))[2];
                            string tel = (address[9].Replace("\"", "").Split(':'))[2];
                            string pro = (address[11].Replace("\"", "").Split(':'))[2];
                            string city = (address[13].Replace("\"", "").Split(':'))[2];
                            string area = (address[15].Replace("\"", "").Split(':'))[2];
                            string addr = (address[17].Replace("\"", "").Split(':'))[2];
                            string zipcode = (address[21].Replace("\"", "").Split(':'))[2];


                            orderItem.merchantOrderId = dt.Rows[i]["ordersn"].ToString();
                            orderItem.addrCountry = "中国";
                            orderItem.addrProvince = pro;
                            orderItem.addrCity = city;
                            orderItem.addrDistrict = area;
                            orderItem.addrDetail = addr;
                            orderItem.distribution = distributionCode;
                            orderItem.consigneeCode = dt.Rows[i]["consigneeCode"].ToString();
                            orderItem.purchase = purchaserCode;
                            orderItem.purchaseId = userDt.Rows[0]["id"].ToString();
                            orderItem.tradeTime = GetTime(dt.Rows[i]["createtime"].ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                            orderItem.consigneeMobile = tel;
                            orderItem.payType = dt.Rows[i]["paytype1"].ToString();
                            orderItem.payNo = dt.Rows[i]["transid"].ToString();
                            double.TryParse(dt.Rows[i]["dispatchprice"].ToString(), out orderItem.freight);

                            string[] idnum = dt.Rows[i]["diyformdata"].ToString().Split(';');
                            if (idnum.Length > 2)
                            {
                                orderItem.consigneeName = (idnum[3].Replace("\"", "").Split(':'))[2]; ;
                                orderItem.idNumber = (idnum[1].Replace("\"", "").Split(':'))[2].ToUpper();
                            }

                            DatabaseOperationWeb.TYPE = bbc;
                            string sql1 = "select g.productsn,g.title,g.costprice,o.* " +
                              "from ims_ewei_shop_order_goods o ,ims_ewei_shop_goods g " +
                              "where o.goodsid =g.id and  o.orderid = " + dt.Rows[i]["id"].ToString();
                            DataTable dt1 = DatabaseOperationWeb.ExecuteSelectDS(sql1, "ims_ewei_shop_order").Tables[0];
                            orderItem.OrderGoods = new List<OrderGoodsItem>();
                            for (int j = 0; j < dt1.Rows.Count; j++)
                            {
                                OrderGoodsItem orderGoodsItem = new OrderGoodsItem();
                                orderGoodsItem.barCode = dt1.Rows[j]["productsn"].ToString();
                                orderGoodsItem.skuBillName = dt1.Rows[j]["title"].ToString();
                                orderGoodsItem.quantity = Convert.ToDouble(dt1.Rows[j]["total"]);
                                orderGoodsItem.skuUnitPrice = Convert.ToDouble(dt1.Rows[j]["price"].ToString()) / Convert.ToDouble(dt1.Rows[j]["total"].ToString());
                                orderItem.OrderGoods.Add(orderGoodsItem);
                            }
                            OrderItemList.Add(orderItem);

                            DatabaseOperationWeb.TYPE = b2b;
                        }
                        else
                        {
                            string sql = "update ims_ewei_shop_order set virtual_info = '-1' where id =  '" + dt.Rows[i]["id"].ToString() + "'";
                            bbcAL.Add(sql);
                            string sql2 = "insert into t_log_bbc_order(ordersn,status,virtual_info,flag,remark) " +
                                          "values('" + dt.Rows[i]["ordersn"].ToString() + "'," +
                                          "'" + dt.Rows[i]["status"].ToString() + "'," +
                                          "'" + dt.Rows[i]["virtual_info"].ToString() + "','0','没有对应代理账号')";
                            b2bAL.Add(sql2);
                        }
                    }
                    else if (dt.Rows[i]["virtual_info"].ToString() == "0")
                    {
                        //b2b推订单过来支付的情况，修改b2b订单状态
                        string sql = "update t_order_list set " +
                            "payType='"+ dt.Rows[i]["paytype"].ToString() + "'," +
                            "payNo='" + dt.Rows[i]["transid"].ToString() + "' , " +
                            "status='1' " +
                            "where parentOrderId ='"+ dt.Rows[i]["ordersn"].ToString() + "'";
                        b2bAL.Add(sql);
                        string sql1 = "update ims_ewei_shop_order set virtual_info = '1' where id =  '" + dt.Rows[i]["id"].ToString() + "'";
                        bbcAL.Add(sql1);
                        string sql2 = "insert into t_log_bbc_order(ordersn,status,virtual_info,flag,remark) " +
                                      "values('" + dt.Rows[i]["ordersn"].ToString() + "'," +
                                      "'" + dt.Rows[i]["status"].ToString() + "'," +
                                      "'" + dt.Rows[i]["virtual_info"].ToString() + "','1','修改状态')";
                        b2bAL.Add(sql2);
                    }
                    else
                    {
                        //暂时没有这种情况
                    }
                }
                else if (dt.Rows[i]["status"].ToString() == "2")//已发货
                {
                    //暂时没有这种情况
                }
                else if (dt.Rows[i]["status"].ToString() == "3")//成功
                {
                    if (dt.Rows[i]["virtual_info"].ToString() == "")//未处理过
                    {
                        //暂时没有这种情况
                    }
                    else
                    {
                        //修改b2b里的订单状态。
                        string sql = "update t_order_list set  status='5' " +
                            "where parentOrderId ='" + dt.Rows[i]["ordersn"].ToString() + "'";
                        b2bAL.Add(sql);
                        string sql1 = "update ims_ewei_shop_order set virtual_info = '3' where id =  '" + dt.Rows[i]["id"].ToString() + "'";
                        bbcAL.Add(sql1);
                        string sql2 = "insert into t_log_bbc_order(ordersn,status,virtual_info,flag,remark) " +
                                      "values('" + dt.Rows[i]["ordersn"].ToString() + "'," +
                                      "'" + dt.Rows[i]["status"].ToString() + "'," +
                                      "'" + dt.Rows[i]["virtual_info"].ToString() + "','1','修改状态')";
                        b2bAL.Add(sql2);
                    }
                }
                else if (dt.Rows[i]["status"].ToString() == "4")//申请退单
                {
                    if (dt.Rows[i]["virtual_info"].ToString() == "")//未处理过
                    {
                        //暂时没有这种情况
                    }
                    else
                    {
                        //修改订单状态，并提醒运营处理 --等b2b退单功能上了以后再补、
                    }
                }
            }
            #endregion

            #region 检查项

            Dictionary<string, string> errorDictionary = new Dictionary<string, string>();
            foreach (OrderItem orderItem in OrderItemList)
            {
                string error = "";
                //判断订单是否已经存在
                string sqlno = "select id from t_order_list where merchantOrderId = '" + orderItem.merchantOrderId + "' or  parentOrderId = '" + orderItem.merchantOrderId + "'";
                DataTable dtno = DatabaseOperationWeb.ExecuteSelectDS(sqlno, "TABLE").Tables[0];
                if (dtno.Rows.Count > 0)
                {
                    errorDictionary.Add(orderItem.merchantOrderId, "订单已存在!");
                }
                else
                {
                    //判断订单日期是否正确
                    DateTime dtime = DateTime.Now;
                    try
                    {
                        dtime = Convert.ToDateTime(orderItem.tradeTime);
                    }
                    catch
                    {
                        error += "创建时间日期格式填写错误,";
                    }
                    //判断地址是否正确
                    string sqlp = "select provinceid from t_base_provinces where province like '" + orderItem.addrProvince + "%'";
                    DataTable dtp = DatabaseOperationWeb.ExecuteSelectDS(sqlp, "TABLE").Tables[0];
                    if (dtp.Rows.Count > 0)
                    {
                        string provinceid = dtp.Rows[0][0].ToString();
                        string sqlc = "select cityid from t_base_cities  " +
                            "where city like '" + orderItem.addrCity + "%' and provinceid=" + provinceid + "";
                        DataTable dtc = DatabaseOperationWeb.ExecuteSelectDS(sqlc, "TABLE").Tables[0];
                        if (dtc.Rows.Count > 0)
                        {
                            string cityid = dtc.Rows[0][0].ToString();
                            string sqla = "select id from t_base_areas " +
                                "where area ='" + orderItem.addrDistrict + "' and cityid=" + cityid + "";
                            DataTable dta = DatabaseOperationWeb.ExecuteSelectDS(sqla, "TABLE").Tables[0];
                            if (dta.Rows.Count == 0)
                            {
                                error += "收货人区填写错误,";
                            }
                        }
                        else
                        {
                            error += "收货人市填写错误,";
                        }
                    }
                    else
                    {
                        error += "收货人省填写错误,";
                    }
                    //判断商品
                    foreach (OrderGoodsItem orderGoodsItem in orderItem.OrderGoods)
                    {
                        //判断条码是否已经存在
                        string sqltm = "select id,goodsName from t_goods_list where barcode = '" + orderGoodsItem.barCode + "'";
                        DataTable dttm = DatabaseOperationWeb.ExecuteSelectDS(sqltm, "TABLE").Tables[0];
                        if (dttm.Rows.Count == 0)
                        {
                            error += orderGoodsItem.barCode + ":商品条码不存在";
                        }
                    }
                    errorDictionary.Add(orderItem.merchantOrderId, error);
                }
            }
            #endregion

            DatabaseOperationWeb.TYPE = b2b;
            #region 处理因仓库分单
            List<OrderItem> newOrderItemList = new List<OrderItem>();
            foreach (var orderItem in OrderItemList)
            {
                if (errorDictionary[orderItem.merchantOrderId] != "")
                {
                    continue;
                }

                string error = "";
                Dictionary<int, List<OrderGoodsItem>> myDictionary = new Dictionary<int, List<OrderGoodsItem>>();
                foreach (OrderGoodsItem orderGoodsItem in orderItem.OrderGoods)
                {
                    string wsql = "select d.platformId,u.id as userId,u.platformCost,u.platformCostType,u.priceType,u.agentCost,u.usertype,u.ofAgent," +
                                  "d.pprice,d.profitPlatform,d.profitAgent,d.profitDealer,d.profitOther1," +
                                  "d.profitOther1Name,d.profitOther2,d.profitOther2Name,d.profitOther3," +
                                  "d.profitOther3Name,w.id as goodsWarehouseId,w.wid,w.wcode,w.goodsnum,w.inprice,bw.taxation,bw.taxation2," +
                                  "bw.taxation2type,bw.taxation2line,bw.freight,w.suppliercode,g.NW,g.slt " +
                                  "from t_goods_distributor_price d ,t_goods_warehouse w,t_base_warehouse bw," +
                                  "t_goods_list g,t_user_list u   " +
                                  "where u.usercode = d.usercode and g.barcode = d.barcode and w.wid = bw.id " +
                                  "and d.barcode = w.barcode and w.supplierid = d.supplierid and d.wid = bw.id " +
                                  "and d.usercode = '" + orderItem.purchase + "' " +
                                  "and d.barcode = '" + orderGoodsItem.barCode + "' and w.goodsnum >=" + orderGoodsItem.quantity +
                                  " order by w.goodsnum asc";
                    DataTable wdt = DatabaseOperationWeb.ExecuteSelectDS(wsql, "TABLE").Tables[0];
                    int wid = 0;
                    if (wdt.Rows.Count == 1)
                    {
                        wid = Convert.ToInt16(wdt.Rows[0]["wid"]);
                        orderGoodsItem.dr = wdt.Rows[0];
                    }
                    else if (wdt.Rows.Count > 1)
                    {
                        wid = Convert.ToInt16(wdt.Rows[0]["wid"]);
                        orderGoodsItem.dr = wdt.Rows[0];
                        for (int i = 0; i < wdt.Rows.Count; i++)
                        {
                            if (myDictionary.ContainsKey(Convert.ToInt16(wdt.Rows[i]["wid"])))
                            {
                                wid = Convert.ToInt16(wdt.Rows[i]["wid"]);
                                orderGoodsItem.dr = wdt.Rows[i];
                                break;
                            }
                        }
                    }
                    else
                    {
                        error += orderGoodsItem.barCode + "没找到对应默认供货信息，";
                        continue;
                    }
                    if (!myDictionary.ContainsKey(wid))
                    {
                        myDictionary.Add(wid, new List<OrderGoodsItem>());
                    }
                    myDictionary[wid].Add(orderGoodsItem);
                }
                if (error != "")
                {
                    errorDictionary[orderItem.merchantOrderId] += error;
                    continue;
                }
                if (myDictionary.Count() > 1)//一个订单有一个以上仓库和供应商的商品
                {
                    int num = 0;
                    double freight = 0;
                    foreach (var kvp in myDictionary)
                    {
                        if (num == 0)//第一个仓库的订单修改部分字段
                        {
                            orderItem.parentOrderId = orderItem.merchantOrderId;
                            orderItem.merchantOrderId += kvp.Key;
                            orderItem.warehouseId = kvp.Key.ToString();
                            orderItem.OrderGoods = new List<OrderGoodsItem>();
                            double tradeAmount = 0;
                            foreach (var item in kvp.Value)
                            {
                                tradeAmount += Convert.ToDouble(item.skuUnitPrice) * Convert.ToDouble(item.quantity);
                                orderItem.OrderGoods.Add(item);
                            }
                            orderItem.tradeAmount = tradeAmount.ToString();
                            //处理运费(暂时只针对大连32库) -20190104韩
                            if (kvp.Key.ToString() == "42"|| kvp.Key.ToString() == "43" || kvp.Key.ToString() == "57" || kvp.Key.ToString() == "58")
                            {

                            }
                            else
                            {
                                freight = orderItem.freight;
                                orderItem.freight = 0;
                            }
                            newOrderItemList.Add(orderItem);
                        }
                        else//其他仓库的订单新建子订单
                        {
                            OrderItem orderItemNew = new OrderItem();
                            orderItemNew.parentOrderId = orderItem.parentOrderId;
                            orderItemNew.merchantOrderId = orderItem.parentOrderId + kvp.Key;
                            orderItemNew.tradeTime = orderItem.tradeTime;
                            orderItemNew.consigneeName = orderItem.consigneeName;
                            orderItemNew.consigneeMobile = orderItem.consigneeMobile;
                            orderItemNew.idNumber = orderItem.idNumber;
                            orderItemNew.addrCountry = orderItem.addrCountry;
                            orderItemNew.addrProvince = orderItem.addrProvince;
                            orderItemNew.addrCity = orderItem.addrCity;
                            orderItemNew.addrDistrict = orderItem.addrDistrict;
                            orderItemNew.addrDetail = orderItem.addrDetail;
                            orderItemNew.consignorName = orderItem.consignorName;
                            orderItemNew.consignorMobile = orderItem.consignorMobile;
                            orderItemNew.consignorAddr = orderItem.consignorAddr;

                            orderItemNew.distribution = orderItem.distribution;
                            orderItemNew.consigneeCode = orderItem.consigneeCode;
                            orderItemNew.purchaseId = orderItem.purchaseId;
                            orderItemNew.purchase = orderItem.purchase;
                            orderItemNew.payType = orderItem.payType;
                            orderItemNew.payNo = orderItem.payNo;

                            //处理运费(暂时只针对大连32库) -20190104韩
                            if (kvp.Key.ToString() == "42" || kvp.Key.ToString() == "43" || kvp.Key.ToString() == "57" || kvp.Key.ToString() == "58")
                            {
                                orderItemNew.freight = freight;
                                freight = 0;
                            }
                            else
                            {
                                orderItemNew.freight = 0;
                            }


                            orderItemNew.OrderGoods = new List<OrderGoodsItem>();
                            double tradeAmount = 0;
                            foreach (var item in kvp.Value)
                            {
                                tradeAmount += Convert.ToDouble(item.skuUnitPrice) * Convert.ToDouble(item.quantity);
                                orderItemNew.OrderGoods.Add(item);
                            }
                            orderItemNew.tradeAmount = tradeAmount.ToString();
                            newOrderItemList.Add(orderItemNew);
                        }
                        num++;
                    }
                }
                else
                {
                    double tradeAmount = 0;
                    foreach (OrderGoodsItem orderGoodsItem in orderItem.OrderGoods)
                    {
                        tradeAmount += Convert.ToDouble(orderGoodsItem.skuUnitPrice) * Convert.ToDouble(orderGoodsItem.quantity);
                    }
                    orderItem.parentOrderId = orderItem.merchantOrderId;
                    orderItem.tradeAmount = tradeAmount.ToString();
                    newOrderItemList.Add(orderItem);
                }
            }
            #endregion

            #region 价格分拆
            string userSql2 = "select * from t_user_list";
            DataTable userDT2 = DatabaseOperationWeb.ExecuteSelectDS(userSql2, "TABLE").Tables[0];
            ArrayList al = new ArrayList();
            ArrayList goodsNumAl = new ArrayList();
            foreach (var orderItem in newOrderItemList)
            {
                if (errorDictionary[orderItem.parentOrderId] != "")
                {
                    continue;
                }
                double freight = 0, tradeAmount = 1;
                double.TryParse(orderItem.OrderGoods[0].dr["freight"].ToString(), out freight);
                double.TryParse(orderItem.tradeAmount, out tradeAmount);
                orderItem.freight = Math.Round(freight, 2);
                orderItem.platformId = orderItem.OrderGoods[0].dr["platformId"].ToString();
                orderItem.warehouseId = orderItem.OrderGoods[0].dr["wid"].ToString();
                orderItem.warehouseCode = orderItem.OrderGoods[0].dr["wcode"].ToString();
                orderItem.supplier = orderItem.OrderGoods[0].dr["suppliercode"].ToString();
                orderItem.purchaseId = orderItem.OrderGoods[0].dr["userId"].ToString();

                double fr = Math.Round(orderItem.freight / tradeAmount, 4);

                //处理供货代理提点
                double supplierAgentCost = 0;
                DataRow[] drs = userDT2.Select("userCode = '" + orderItem.supplier + "'");
                if (drs.Length > 0)
                {
                    if (drs[0]["ofAgent"].ToString() != "")
                    {
                        orderItem.supplierAgentCode = drs[0]["ofAgent"].ToString();
                        double.TryParse(drs[0]["agentCost"].ToString(), out supplierAgentCost);
                    }
                }

                //处理采购代理提点
                double purchaseAgentCost = 0;
                if (orderItem.OrderGoods[0].dr["ofAgent"].ToString() != "")
                {
                    //分销商需要看分销代理有没有采购代理。
                    if (orderItem.OrderGoods[0].dr["usertype"].ToString() == "4")
                    {
                        DataRow[] drs1 = userDT2.Select("userCode = '" + orderItem.OrderGoods[0].dr["ofAgent"].ToString() + "'");
                        if (drs1.Length > 0)
                        {
                            if (drs1[0]["ofAgent"].ToString() != "")
                            {
                                orderItem.purchaseAgentCode = drs1[0]["ofAgent"].ToString();
                                double.TryParse(drs1[0]["agentCost"].ToString(), out purchaseAgentCost);
                            }
                        }
                    }
                    else
                    {
                        orderItem.purchaseAgentCode = orderItem.OrderGoods[0].dr["ofAgent"].ToString();
                        double.TryParse(orderItem.OrderGoods[0].dr["agentCost"].ToString(), out purchaseAgentCost);
                    }
                }
                for (int i = 0; i < orderItem.OrderGoods.Count; i++)
                {
                    OrderGoodsItem orderGoodsItem = orderItem.OrderGoods[i];
                    //处理运费
                    //if (i== orderItem.OrderGoods.Count-1)
                    //{
                    //    orderGoodsItem.waybillPrice = freight;
                    //}
                    //else
                    //{
                    //    orderGoodsItem.waybillPrice = Math.Round(fr * orderGoodsItem.totalPrice,2);
                    //    freight -= orderGoodsItem.waybillPrice;
                    //}
                    //从运费平摊修改为运费都为全部运费。
                    //orderGoodsItem.waybillPrice = freight;


                    //处理供货价和销售价和供货商code
                    orderGoodsItem.supplyPrice = Math.Round(Convert.ToDouble(orderGoodsItem.dr["inprice"]), 2);
                    orderGoodsItem.purchasePrice = Math.Round(Convert.ToDouble(orderGoodsItem.dr["pprice"]), 2);
                    orderGoodsItem.suppliercode = orderGoodsItem.dr["suppliercode"].ToString();
                    orderGoodsItem.slt = orderGoodsItem.dr["slt"].ToString();
                    orderGoodsItem.totalPrice = orderGoodsItem.skuUnitPrice * orderGoodsItem.quantity;
                    string goodsWarehouseId = orderGoodsItem.dr["goodsWarehouseId"].ToString();//库存id
                                                                                               //处理税
                    double taxation = 0;
                    double.TryParse(orderGoodsItem.dr["taxation"].ToString(), out taxation);
                    if (taxation > 0)
                    {
                        double taxation2 = 0, taxation2type = 0, taxation2line = 0, nw = 0;
                        double.TryParse(orderGoodsItem.dr["taxation2"].ToString(), out taxation2);
                        double.TryParse(orderGoodsItem.dr["taxation2type"].ToString(), out taxation2type);
                        double.TryParse(orderGoodsItem.dr["taxation2line"].ToString(), out taxation2line);
                        double.TryParse(orderGoodsItem.dr["NW"].ToString(), out nw);
                        if (taxation2 == 0)
                        {
                            orderGoodsItem.tax = orderGoodsItem.totalPrice * taxation / 100;
                        }
                        else
                        {
                            if (taxation2type == 1)//按总价提档
                            {
                                if (orderGoodsItem.skuUnitPrice > taxation2line)//价格过线
                                {
                                    orderGoodsItem.tax = orderGoodsItem.totalPrice * taxation2 / 100;
                                }
                                else//价格没过线
                                {
                                    orderGoodsItem.tax = orderGoodsItem.totalPrice * taxation / 100;
                                }
                            }
                            else if (taxation2type == 2)//按元/克提档
                            {
                                if (nw == 0)//如果没有净重，按默认税档
                                {
                                    orderGoodsItem.tax = orderGoodsItem.totalPrice * taxation / 100;
                                }
                                else
                                {
                                    if (orderGoodsItem.skuUnitPrice / (nw * 1000) > taxation2line)//价格过线
                                    {
                                        orderGoodsItem.tax = orderGoodsItem.totalPrice * taxation2 / 100;
                                    }
                                    else//价格没过线
                                    {
                                        orderGoodsItem.tax = orderGoodsItem.totalPrice * taxation / 100;
                                    }
                                }
                                //还要考虑面膜的问题
                            }
                            else//都不是按初始税率走
                            {
                                orderGoodsItem.tax = orderGoodsItem.totalPrice * taxation / 100;
                            }
                        }
                    }
                    else
                    {
                        orderGoodsItem.tax = 0;
                    }
                    orderGoodsItem.tax = Math.Round(orderGoodsItem.tax, 2);
                    //处理平台提点
                    orderGoodsItem.platformPrice = 0;
                    double platformCost = 0;
                    double.TryParse(orderGoodsItem.dr["platformCost"].ToString(), out platformCost);
                    if (platformCost > 0)
                    {
                        if (orderGoodsItem.dr["platformCostType"].ToString() == "1")//进价计算
                        {
                            orderGoodsItem.platformPrice = orderGoodsItem.supplyPrice * orderGoodsItem.quantity * platformCost / 100;
                        }
                        else if (orderGoodsItem.dr["platformCostType"].ToString() == "2")//售价计算
                        {
                            if (orderGoodsItem.dr["priceType"].ToString() == "1")//按订单售价计算
                            {
                                orderGoodsItem.platformPrice = orderGoodsItem.totalPrice * platformCost / 100;
                            }
                            else if (orderGoodsItem.dr["priceType"].ToString() == "2")//按供货价计算
                            {
                                orderGoodsItem.platformPrice = orderGoodsItem.purchasePrice * orderGoodsItem.quantity * platformCost / 100;
                            }
                        }
                    }


                    //处理供货代理提点
                    orderGoodsItem.supplierAgentPrice = 0;
                    if (supplierAgentCost > 0)
                    {
                        //按供货价计算
                        orderGoodsItem.supplierAgentPrice = Math.Round(orderGoodsItem.supplyPrice * orderGoodsItem.quantity * supplierAgentCost / 100, 2);
                        orderGoodsItem.supplierAgentCode = orderItem.supplierAgentCode;
                        orderGoodsItem.supplierAgentPrice = Math.Round(orderGoodsItem.supplierAgentPrice, 2);
                    }
                    //处理采购代理提点
                    orderGoodsItem.purchaseAgentPrice = 0;
                    if (purchaseAgentCost > 0)
                    {
                        if (orderGoodsItem.dr["platformCostType"].ToString() == "1")//进价计算
                        {
                            orderGoodsItem.purchaseAgentPrice = orderGoodsItem.supplyPrice * orderGoodsItem.quantity * purchaseAgentCost / (100 - platformCost);
                        }
                        else if (orderGoodsItem.dr["platformCostType"].ToString() == "2")//售价计算
                        {
                            if (orderGoodsItem.dr["priceType"].ToString() == "1")//按订单售价计算
                            {
                                orderGoodsItem.purchaseAgentPrice = orderGoodsItem.totalPrice * purchaseAgentCost / 100;
                            }
                            else if (orderGoodsItem.dr["priceType"].ToString() == "2")//按供货价计算
                            {
                                orderGoodsItem.purchaseAgentPrice = orderGoodsItem.purchasePrice * orderGoodsItem.quantity * purchaseAgentCost / 100;
                            }
                        }
                        orderGoodsItem.purchaseAgentPrice = Math.Round(orderGoodsItem.purchaseAgentPrice, 2);
                    }


                    orderGoodsItem.platformPrice = Math.Round(orderGoodsItem.platformPrice, 2);
                    //判断误差，误差=供货价-进价-平台提点-供货代理提点-采购代理提点-运费分成-税
                    double deviation = orderGoodsItem.purchasePrice * orderGoodsItem.quantity
                        - orderGoodsItem.supplyPrice * orderGoodsItem.quantity
                        - orderGoodsItem.platformPrice
                        - orderGoodsItem.supplierAgentPrice
                        - orderGoodsItem.purchaseAgentPrice
                        - orderGoodsItem.waybillPrice
                        - orderGoodsItem.tax;
                    //如果有误差了，就从平台提点扣除
                    if (deviation != 0)
                    {
                        if (orderGoodsItem.platformPrice + deviation > 0)
                        {
                            orderGoodsItem.platformPrice = orderGoodsItem.platformPrice + deviation;
                        }
                        else
                        {
                           // msg.msg = "订单" + orderItem.merchantOrderId + "价格有误差，请查对！";
                        }
                    }
                    //处理利润
                    //利润=售价-供货价
                    double profit = (orderGoodsItem.skuUnitPrice - orderGoodsItem.purchasePrice) * orderGoodsItem.quantity;
                    double profitPlatform = 0, profitAgent = 0, profitDealer = 0, profitOther1 = 0, profitOther2 = 0, profitOther3 = 0;
                    double.TryParse(orderGoodsItem.dr["profitPlatform"].ToString(), out profitPlatform);
                    double.TryParse(orderGoodsItem.dr["profitAgent"].ToString(), out profitAgent);
                    double.TryParse(orderGoodsItem.dr["profitDealer"].ToString(), out profitDealer);
                    double.TryParse(orderGoodsItem.dr["profitOther1"].ToString(), out profitOther1);
                    double.TryParse(orderGoodsItem.dr["profitOther2"].ToString(), out profitOther2);
                    double.TryParse(orderGoodsItem.dr["profitOther3"].ToString(), out profitOther3);
                    orderGoodsItem.profitPlatform = Math.Round(profit * profitPlatform / 100, 2);
                    orderGoodsItem.profitAgent = Math.Round(profit * profitAgent / 100, 2);
                    orderGoodsItem.profitDealer = Math.Round(profit * profitDealer / 100, 2);
                    orderGoodsItem.profitOther1 = Math.Round(profit * profitOther1 / 100, 2);
                    orderGoodsItem.profitOther2 = Math.Round(profit * profitOther2 / 100, 2);
                    orderGoodsItem.profitOther3 = Math.Round(profit * profitOther3 / 100, 2);
                    double x = Math.Round(profit - orderGoodsItem.profitPlatform - orderGoodsItem.profitAgent
                                                    - orderGoodsItem.profitDealer - orderGoodsItem.profitOther1
                                                    - orderGoodsItem.profitOther2 - orderGoodsItem.profitOther3, 2);
                    if (x!=0)
                    {
                        orderGoodsItem.profitAgent = orderGoodsItem.profitAgent + x;
                    }
                    
                    orderGoodsItem.other1Name = orderGoodsItem.dr["profitOther1Name"].ToString();
                    orderGoodsItem.other2Name = orderGoodsItem.dr["profitOther2Name"].ToString();
                    orderGoodsItem.other3Name = orderGoodsItem.dr["profitOther3Name"].ToString();

                    string sqlgoods = "insert into t_order_goods(merchantOrderId,barCode,slt,skuUnitPrice," +
                                  "quantity,skuBillName,batchNo,goodsName," +
                                  "api,fqSkuID,sendType,status," +
                                  "suppliercode,supplyPrice,purchasePrice,waybill," +
                                  "waybillPrice,tax,platformPrice,profitPlatform," +
                                  "supplierAgentPrice,supplierAgentCode,purchaseAgentPrice,purchaseAgentCode," +
                                  "profitAgent,profitDealer,profitOther1,other1Name," +
                                  "profitOther2,other2Name,profitOther3,other3Name) " +
                                  "values('" + orderItem.merchantOrderId + "','" + orderGoodsItem.barCode + "','" + orderGoodsItem.slt + "','" + orderGoodsItem.skuUnitPrice + "'" +
                                  ",'" + orderGoodsItem.quantity + "','" + orderGoodsItem.skuBillName + "','','" + orderGoodsItem.skuBillName + "'" +
                                  ",'','','','0'" +
                                  ",'" + orderGoodsItem.suppliercode + "','" + orderGoodsItem.supplyPrice + "','" + orderGoodsItem.purchasePrice + "',''" +
                                  ",'" + orderGoodsItem.waybillPrice + "','" + orderGoodsItem.tax + "','" + orderGoodsItem.platformPrice + "','" + orderGoodsItem.profitPlatform + "'" +
                                  ",'" + orderGoodsItem.supplierAgentPrice + "','" + orderGoodsItem.supplierAgentCode + "','" + orderGoodsItem.purchaseAgentPrice + "','" + orderGoodsItem.purchaseAgentCode + "'" +
                                  ",'" + orderGoodsItem.profitAgent + "','" + orderGoodsItem.profitDealer + "','" + orderGoodsItem.profitOther1 + "','" + orderGoodsItem.other1Name + "'" +
                                  ",'" + orderGoodsItem.profitOther2 + "','" + orderGoodsItem.other2Name + "','" + orderGoodsItem.profitOther3 + "','" + orderGoodsItem.other3Name + "'" +
                                  ")";
                    al.Add(sqlgoods);
                    string upsql = "update t_goods_warehouse set goodsnum = goodsnum-" + orderGoodsItem.quantity + " where id = " + goodsWarehouseId;
                    goodsNumAl.Add(upsql);
                    string logsql = "insert into t_log_goodsnum(inputType,createtime,wid,wcode,orderid,barcode,goodsnum,state) " +
                                    "values('',now(),'" + orderItem.warehouseId + "','" + orderItem.warehouseCode + "'," +
                                    "'" + orderItem.merchantOrderId + "','" + orderGoodsItem.barCode + "'," +
                                    "" + orderGoodsItem.quantity + ",'" + orderItem.status + "')";
                    goodsNumAl.Add(logsql);
                }
                string sqlorder = "insert into t_order_list(warehouseId,warehouseCode,customerCode,actionType," +
                    "orderType,serviceType,parentOrderId,merchantOrderId," +
                    "payType,payNo,tradeTime,consigneeCode," +
                    "tradeAmount,goodsTotalAmount,consigneeName,consigneeMobile," +
                    "addrCountry,addrProvince,addrCity,addrDistrict," +
                    "addrDetail,zipCode,idType,idNumber," +
                    "idFountImgUrl,idBackImgUrl,status,purchaserCode," +
                    "purchaserId,distributionCode,apitype,waybillno," +
                    "expressId,inputTime,fqID,supplierAgentCode,purchaseAgentCode," +
                    "operate_status,sendapi,platformId,consignorName," +
                    "consignorMobile,consignorAddr,batchid,outNo,waybillOutNo," +
                    "accountsStatus,accountsNo,prePayId,ifPrint,printNo,freight) " +
                    "values('" + orderItem.warehouseId + "','" + orderItem.warehouseCode + "','" + orderItem.supplier + "',''" +
                    ",'','','" + orderItem.parentOrderId + "','" + orderItem.merchantOrderId + "'" +
                    ",'" + orderItem.payType + "','" + orderItem.payNo + "','" + orderItem.tradeTime + "','" + orderItem.consigneeCode + "'" +
                    "," + orderItem.tradeAmount + ",'" + orderItem.tradeAmount + "','" + orderItem.consigneeName + "','" + orderItem.consigneeMobile + "'" +
                    ",'" + orderItem.addrCountry + "','" + orderItem.addrProvince + "','" + orderItem.addrCity + "','" + orderItem.addrDistrict + "'" +
                    ",'" + orderItem.addrDetail + "','','1','" + orderItem.idNumber + "'" +
                    ",'','','1','" + orderItem.purchase + "'" +
                    ",'" + orderItem.purchaseId + "','" + orderItem.distribution + "','1',''" +
                    ",'',now(),'','" + orderItem.supplierAgentCode + "','" + orderItem.purchaseAgentCode + "'" +
                    ",'0','','" + orderItem.platformId + "','" + orderItem.consignorName + "'" +
                    ",'" + orderItem.consignorMobile + "','" + orderItem.consignorAddr + "','','',''" +
                    ",'0','','','0','','" + orderItem.freight + "') ";
                al.Add(sqlorder);
            }

            #endregion

            if (DatabaseOperationWeb.ExecuteDML(al))
            {
                DatabaseOperationWeb.ExecuteDML(goodsNumAl);
                int errNum = 0;
                foreach (var kvp in errorDictionary)
                {
                    if (kvp.Value != "")
                    {
                        string sql2 = "insert into t_log_bbc_order(ordersn,status,virtual_info,flag,remark) " +
                                      "values('" + kvp.Key + "'," +
                                      "'1'," +
                                      "'','0','"+ kvp.Value + "')";
                        b2bAL.Add(sql2);
                    }
                    else
                    {
                        string sql = "update ims_ewei_shop_order set virtual_info = '1' where ordersn =  '" + kvp.Key + "'";
                        bbcAL.Add(sql);
                        string sql2 = "insert into t_log_bbc_order(ordersn,status,virtual_info,flag,remark) " +
                                      "values('" + kvp.Key + "','1','','1','')";
                        b2bAL.Add(sql2);
                    }
                }
                if (b2bAL.Count > 0)
                {
                    DatabaseOperationWeb.ExecuteDML(b2bAL);
                }
                if (bbcAL.Count > 0)
                {
                    DatabaseOperationWeb.TYPE = bbc;
                    DatabaseOperationWeb.ExecuteDML(bbcAL);
                }
            }
        }
        /// <summary>
        /// 添加订单日志
        /// </summary>
        /// <param name="ordersn">订单号</param>
        /// <param name="status">bbc中的状态</param>
        /// <param name="virtualInfo">同步状态</param>
        /// <param name="flag">0失败，1成功</param>
        /// <param name="remark">错误信息</param>
        private void insertBBCOrderLog(string ordersn,string status,string virtualInfo,string flag,string remark)
        {
            IType itype = DatabaseOperationWeb.TYPE;
            B2BDBManager b2b = new B2BDBManager();
            DatabaseOperationWeb.TYPE = b2b;
            string sql = "insert into t_log_bbc_order(ordersn,status,virtual_info,flag,remark) " +
                         "values('" + ordersn + "','" + status + "','" + virtualInfo + "','" + flag + "','" + remark + "')";
            DatabaseOperationWeb.ExecuteDML(sql);
            DatabaseOperationWeb.TYPE = itype;
        }
         
        #region 旧代码
        private string initBBC(DataTable dt)
        {
            string error = "";
            ArrayList al = new ArrayList();
            Dictionary<string, OrderBean> dict = new Dictionary<string, OrderBean>();
            //List<OrderBean> oblist = new List<OrderBean>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //判断来源
                B2BDBManager b2b = new B2BDBManager();
                DatabaseOperationWeb.TYPE = b2b;
                string purchaserCode = "", distributionCode = "";
                string userSql = "select usercode from t_user_list where uniacid ='" + dt.Rows[i]["uniacid"].ToString() + "'";
                DataTable userDt = DatabaseOperationWeb.ExecuteSelectDS(userSql, "t_user_list").Tables[0];
                if (userDt.Rows.Count > 0)
                {
                    purchaserCode = userDt.Rows[0]["usercode"].ToString();

                    string userSql1 = "select usercode from t_user_list where agentid = '" + dt.Rows[i]["agentid"].ToString() + "'";
                    DataTable userDt1 = DatabaseOperationWeb.ExecuteSelectDS(userSql1, "ims_ewei_shop_order").Tables[0];
                    if (userDt1.Rows.Count > 0)
                    {
                        distributionCode = userDt.Rows[0][0].ToString();
                    }
                    else
                    {
                        error += dt.Rows[i]["id"].ToString() + "无对应分销商！\r\n";
                    }
                }

                BBCDBManager bbc = new BBCDBManager();
                DatabaseOperationWeb.TYPE = bbc;
                DatabaseOperationWeb.ExecuteDML(al);



                string sql = "update ims_ewei_shop_order set virtual_info = '" + dt.Rows[i]["status"].ToString() + "' where id =  '" + dt.Rows[i]["id"].ToString() + "'";
                al.Add(sql);
                string[] address = dt.Rows[i]["address"].ToString().Split(';');
                string name = (address[7].Replace("\"", "").Split(':'))[2];
                string tel = (address[9].Replace("\"", "").Split(':'))[2];
                string pro = (address[11].Replace("\"", "").Split(':'))[2];
                string city = (address[13].Replace("\"", "").Split(':'))[2];
                string area = (address[15].Replace("\"", "").Split(':'))[2];
                string addr = (address[17].Replace("\"", "").Split(':'))[2];
                string zipcode = (address[21].Replace("\"", "").Split(':'))[2];

                string cardnum = "";
                string[] idnum = dt.Rows[i]["diyformdata"].ToString().Split(';');
                if (idnum.Length > 2)
                {
                    name = (idnum[3].Replace("\"", "").Split(':'))[2];
                    cardnum = (idnum[1].Replace("\"", "").Split(':'))[2].ToUpper();
                }

                string status = dt.Rows[i]["status"].ToString();
                if (status == "1")
                {
                    status = "新订单";
                }
                else if (status == "2")
                {
                    status = "已发货";
                }
                else if (status == "3")
                {
                    status = "已完成";
                }
                else if (status == "-1")
                {
                    status = "已关闭";
                }
                OrderBean ob = new OrderBean("", "", dt.Rows[i]["ordersn"].ToString(), dt.Rows[i]["ordersn"].ToString(),
                    GetTime(dt.Rows[i]["createtime"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"), Convert.ToDouble(dt.Rows[i]["price"].ToString()),
                   Convert.ToDouble(dt.Rows[i]["goodsprice"].ToString()), name, tel, "中国", pro, city, area, addr, zipcode, "1", cardnum, "", "",
                   status, purchaserCode, distributionCode, "1", "", "BBC","0", null);
                string sql1 = "select g.productsn,g.title,g.costprice,o.* " +
                              "from ims_ewei_shop_order_goods o ,ims_ewei_shop_goods g " +
                              "where o.goodsid =g.id and  o.orderid = " + dt.Rows[i]["id"].ToString();
                DataTable dt1 = DatabaseOperationWeb.ExecuteSelectDS(sql1, "ims_ewei_shop_order").Tables[0];
                List<GoodsBean> lg = new List<GoodsBean>();
                for (int j = 0; j < dt1.Rows.Count; j++)
                {
                    GoodsBean gb = new GoodsBean("", dt.Rows[i]["ordersn"].ToString(), dt1.Rows[j]["productsn"].ToString(), Convert.ToDouble(dt1.Rows[j]["price"].ToString()) / Convert.ToDouble(dt1.Rows[j]["total"].ToString()),
                        Convert.ToInt32(dt1.Rows[j]["total"].ToString()), dt1.Rows[j]["title"].ToString(), "BBC", dt1.Rows[j]["title"].ToString(), Convert.ToDouble(dt1.Rows[j]["costprice"].ToString()), 0);
                    lg.Add(gb);
                }
                ob.GoodsList = lg;
                dict.Add(dt.Rows[i]["ordersn"].ToString(), ob);
            }

            try
            {
                B2BDBManager b2b = new B2BDBManager();
                DatabaseOperationWeb.TYPE = b2b;
                insertOrder(dict);
                BBCDBManager bbc = new BBCDBManager();
                DatabaseOperationWeb.TYPE = bbc;
                DatabaseOperationWeb.ExecuteDML(al);
            }
            catch (Exception)
            {
                error = "导入过程发生错误！";
            }


            return error;
        }
        /// <summary>
        /// 时间戳转为C#格式时间
        /// </summary>
        /// <param name="timeStamp">Unix时间戳格式</param>
        /// <returns>C#格式时间</returns>
        public static DateTime GetTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }
        #region 处理订单数据
        private void insertOrder(Dictionary<string, OrderBean> dict)
        {
            //foreach (OrderBean order in dict.Values)
            //{
            //    string sql = "select * from t_order_list where parentOrderId ='"+order.ParentOrderId+"'";
            //    DataTable dt = DatabaseOperationWeb.ExecuteSelectDS(sql, "t_order_list").Tables[0];
            //    if (dt.Rows.Count==0)//没有的话进行分仓处理
            //    {
            //        saveOrder(order);
            //        saveOrderGoods(order.WarehouseCode, order.GoodsList);
            //    }
            //    else
            //    {

            //    }

            //}
            string cksql = "select * from t_ck_goods_warehouse";
            DataTable CKGOODSDT = DatabaseOperationWeb.ExecuteSelectDS(cksql, "t_ck_goods_warehouse").Tables["t_ck_goods_warehouse"];
            string goodssql = "select * from t_goods_list where ifBBC='1' ";
            DataTable goodsDT = DatabaseOperationWeb.ExecuteSelectDS(goodssql, "t_goods_list").Tables["t_goods_list"];

            string sql1 = "select wcode,wname,orderCode from t_base_warehouse where if_CK = '1' ";
            DataTable WHDT = DatabaseOperationWeb.ExecuteSelectDS(sql1, "t_order_list").Tables["t_order_list"];

            foreach (OrderBean order in dict.Values)
            {
                //DataTable beanDT = WHDT.Copy();
                //beanDT.Columns.Add("goodslist");
                Dictionary<string, List<GoodsBean>> whdict = new Dictionary<string, List<GoodsBean>>();
                for (int i = 0; i < WHDT.Rows.Count; i++)
                {
                    whdict.Add(WHDT.Rows[i]["wcode"].ToString(), null);
                }

                //beanDT.Columns.Add("totalprice");
                bool ifDCL = false;//是否是待处理的
                foreach (GoodsBean goodsBean in order.GoodsList)
                {
                    DataRow[] drs = CKGOODSDT.Select("barcode = '" + goodsBean.BarCode + "'");
                    if (drs.Length == 1)//只要有分配到两个仓的或者一个仓有俩不同价钱的，就待处理。
                    {
                        if (whdict.ContainsKey(drs[0]["wcode"].ToString()))
                        {
                            //判断下在普通商品表里有没有该商品，如果有就待处理 2018-04-26新增
                            DataRow[] goodsdrs = goodsDT.Select("barcode = '" + goodsBean.BarCode + "'");
                            if (goodsdrs.Length > 0)
                            {
                                ifDCL = true;
                                break;
                            }
                            //如果判断库存是否大于订单商品数量，如果少于商品数量就待处理
                            if (Convert.ToInt16(drs[0]["goodsnum"]) > goodsBean.Quantity)
                            {
                                //如果判断减去订单商品数量后，库存少于警戒线数值就发短信
                                if (Convert.ToInt16(drs[0]["goodsnum"]) - goodsBean.Quantity < CKConfig.smsNum)
                                {
                                    CKConfig.sms(goodsBean.BarCode);
                                }

                                //goodsBean.PurchasePrice = Convert.ToDouble(drs[0]["inprice"]);
                                if (whdict[drs[0]["wcode"].ToString()] == null)
                                {
                                    //List<GoodsBean> list = (List<GoodsBean>)beanDT.Rows[i]["goodslist"];
                                    List<GoodsBean> list = new List<GoodsBean>();
                                    list.Add(goodsBean);
                                    whdict[drs[0]["wcode"].ToString()] = list;
                                }
                                else
                                {
                                    whdict[drs[0]["wcode"].ToString()].Add(goodsBean);
                                }
                            }
                            else
                            {
                                ifDCL = true;
                                break;
                            }
                        }
                        else
                        {
                            ifDCL = true;
                            break;
                        }
                    }
                    else if (drs.Length == 0)//如果仓库商品表找不到，就去普通商品表找
                    {
                        DataRow[] goodsdrs = goodsDT.Select("barcode = '" + goodsBean.BarCode + "'");
                        if (goodsdrs.Length > 0)
                        {
                            if (whdict.ContainsKey(goodsdrs[0]["wcode"].ToString()))
                            {
                                //如果判断库存是否大于订单商品数量，如果少于商品数量就待处理
                                if (Convert.ToInt16(goodsdrs[0]["stock"]) > goodsBean.Quantity)
                                {
                                    //如果判断减去订单商品数量后，库存少于警戒线数值就发短信
                                    if (Convert.ToInt16(goodsdrs[0]["stock"]) - goodsBean.Quantity < CKConfig.smsNum)
                                    {
                                        CKConfig.sms(goodsBean.BarCode);
                                    }

                                    //goodsBean.PurchasePrice = Convert.ToDouble(goodsdrs[0]["purchasePrice"]);
                                    if (whdict[goodsdrs[0]["wcode"].ToString()] == null)
                                    {
                                        //List<GoodsBean> list = (List<GoodsBean>)beanDT.Rows[i]["goodslist"];
                                        List<GoodsBean> list = new List<GoodsBean>();
                                        list.Add(goodsBean);
                                        whdict[goodsdrs[0]["wcode"].ToString()] = list;
                                    }
                                    else
                                    {
                                        whdict[goodsdrs[0]["wcode"].ToString()].Add(goodsBean);
                                    }
                                }
                                else
                                {
                                    ifDCL = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            ifDCL = true;
                            break;
                        }
                    }
                    else
                    {
                        ifDCL = true;
                        break;
                    }
                }
                if (ifDCL)//待处理的
                {
                    order.Status = "待处理";
                    double total = 0;
                    foreach (GoodsBean goodsBean in order.GoodsList)
                    {
                        total += goodsBean.SkuUnitPrice * goodsBean.Quantity;
                    }
                    order.GoodsTotalAmount = total;
                    order.TradeAmount = total;
                    saveOrder(order);
                    saveOrderGoods(order.WarehouseCode, order.GoodsList);
                }
                else
                {
                    foreach (string wcode in whdict.Keys)
                    {
                        if (whdict[wcode] != null)
                        {
                            DataRow[] drs = WHDT.Select("wcode = '" + wcode + "'");
                            string orderCode = drs[0]["orderCode"].ToString();

                            order.WarehouseCode = wcode;
                            //order.MerchantOrderId = order.ParentOrderId + orderCode;
                            order.MerchantOrderId = getOrderId("N") + orderCode;
                            //order.Status = "新订单";
                            double total = 0;
                            foreach (GoodsBean goodsBean in whdict[wcode])
                            {
                                total += goodsBean.SkuUnitPrice * goodsBean.Quantity;
                                goodsBean.MerchantOrderId = order.MerchantOrderId;
                            }
                            order.GoodsTotalAmount = total;
                            order.TradeAmount = total;
                            saveOrder(order);
                            saveOrderGoods(order.WarehouseCode, whdict[wcode]);
                        }
                    }
                }
            }
        }
        private bool saveOrder(OrderBean orderBean)
        {
            string insql = "insert into t_order_list(warehouseCode,parentOrderId,merchantOrderId,tradeTime," +
                                                "tradeAmount,goodsTotalAmount,consigneeName,consigneeMobile," +
                                                "addrCountry,addrProvince,addrCity,addrDistrict," +
                                                "addrDetail,zipCode,idType,idNumber," +
                                                "idFountImgUrl,idBackImgUrl,status,purchaserCode," +
                                                "distributionCode,apitype,fqID,sendapi,freight) " +
                        "values('" + orderBean.WarehouseCode + "','" + orderBean.ParentOrderId + "','" + orderBean.MerchantOrderId + "','" + orderBean.TradeTime + "'," +
                        "'" + orderBean.TradeAmount + "','" + orderBean.GoodsTotalAmount + "','" + orderBean.ConsigneeName + "','" + orderBean.ConsigneeMobile + "'," +
                        "'" + orderBean.AddrCountry + "','" + orderBean.AddrProvince + "','" + orderBean.AddrCity + "','" + orderBean.AddrDistrict + "'," +
                        "'" + orderBean.AddrDetail + "','" + orderBean.ZipCode + "','" + orderBean.IdType + "','" + orderBean.IdNumber + "'," +
                        "'" + orderBean.IdFountImgUrl + "','" + orderBean.IdBackImgUrl + "','" + orderBean.Status + "','" + orderBean.PurchaserCode + "'," +
                        "'" + orderBean.DistributionCode + "','" + orderBean.Apitype + "','" + orderBean.FqID + "','" + orderBean.Sendapi + "','" + orderBean.Freight + "')";
            return DatabaseOperationWeb.ExecuteDML(insql);
        }
        private bool saveOrderGoods(string wcode, List<GoodsBean> goodsBeanList)
        {
            ArrayList al = new ArrayList();
            foreach (GoodsBean goodsBean in goodsBeanList)
            {
                string insql = "insert into t_order_goods(merchantOrderId,barCode,skuUnitPrice,quantity," +
                "                                     goodsName,sendType,skubillname,supplyPrice,purchasePrice) " +
                           "values('" + goodsBean.MerchantOrderId + "','" + goodsBean.BarCode + "'," + goodsBean.SkuUnitPrice + "," + goodsBean.Quantity + "," +
                           "'" + goodsBean.GoodsName + "','" + goodsBean.SendType + "','" + goodsBean.Skubillname + "'," + goodsBean.SupplyPrice + "," + goodsBean.PurchasePrice + ")";
                al.Add(insql);
                if (wcode != "")
                {
                    setGoodsNum(wcode, goodsBean.MerchantOrderId, goodsBean.BarCode, goodsBean.Quantity);
                }
            }
            return DatabaseOperationWeb.ExecuteDML(al);
        }
        private void setGoodsNum(string wcode, string orderid, string tm, int num)
        {
            string sql = "update t_ck_goods_warehouse set goodsnum=goodsnum-" + num + " where barcode = '" + tm + "' and wcode= '" + wcode + "' ";
            if (DatabaseOperationWeb.ExecuteDML(sql))
            {
                setGoodsNumLog(wcode, orderid, tm, num, "扣除库存");
            }
        }

        private void setGoodsNumLog(string wcode, string orderid, string tm, int num, string state)
        {
            string sql = "insert into t_log_goodsnum (createtime,wcode,orderid,tm,goodsnum,state) values(now(),'" + wcode + "','" + orderid + "','" + tm + "'," + num + ",'" + state + "')";
            DatabaseOperationWeb.ExecuteDML(sql);
        }
        #endregion
        private string getOrderId(string code)
        {
            string sql = "select nextval('ORDER')";
            DataTable dt = DatabaseOperationWeb.ExecuteSelectDS(sql, "dual").Tables["dual"];
            if (dt.Rows.Count > 0)
            {
                return DateTime.Now.ToString("yyyyMMddHHmm") + code + dt.Rows[0][0].ToString();
            }
            else
            {
                return null;
            }

        }
        #endregion
    }
    class OrderBean
    {
        private string id;
        private string warehouseCode;
        private string parentOrderId;
        private string merchantOrderId;
        private string tradeTime;
        private double tradeAmount;
        private double goodsTotalAmount;
        private string consigneeName;
        private string consigneeMobile;
        private string addrCountry;
        private string addrProvince;
        private string addrCity;
        private string addrDistrict;
        private string addrDetail;
        private string zipCode;
        private string idType;
        private string idNumber;
        private string idFountImgUrl;
        private string idBackImgUrl;
        private string status;
        private string purchaserCode;
        private string distributionCode;
        private string apitype;
        private string fqID;
        private string sendapi;
        private string freight;
        private List<GoodsBean> goodsList;

        public OrderBean(string id, string warehouseCode, string parentOrderId, string merchantOrderId, string tradeTime, double tradeAmount, double goodsTotalAmount, string consigneeName, string consigneeMobile, string addrCountry, string addrProvince, string addrCity, string addrDistrict, string addrDetail, string zipCode, string idType, string idNumber, string idFountImgUrl, string idBackImgUrl, string status, string purchaserCode, string distributionCode, string apitype, string fqID, string sendapi,string freight, List<GoodsBean> goodsList)
        {
            this.id = id;
            this.warehouseCode = warehouseCode;
            this.parentOrderId = parentOrderId;
            this.merchantOrderId = merchantOrderId;
            this.tradeTime = tradeTime;
            this.tradeAmount = tradeAmount;
            this.goodsTotalAmount = goodsTotalAmount;
            this.consigneeName = consigneeName;
            this.consigneeMobile = consigneeMobile;
            this.addrCountry = addrCountry;
            this.addrProvince = addrProvince;
            this.addrCity = addrCity;
            this.addrDistrict = addrDistrict;
            this.addrDetail = addrDetail;
            this.zipCode = zipCode;
            this.idType = idType;
            this.idNumber = idNumber;
            this.idFountImgUrl = idFountImgUrl;
            this.idBackImgUrl = idBackImgUrl;
            this.status = status;
            this.purchaserCode = purchaserCode;
            this.distributionCode = distributionCode;
            this.apitype = apitype;
            this.fqID = fqID;
            this.sendapi = sendapi;
            this.freight = freight;
            this.goodsList = goodsList;
        }

        public string WarehouseCode { get => warehouseCode; set => warehouseCode = value; }
        public string ParentOrderId { get => parentOrderId; set => parentOrderId = value; }
        public string MerchantOrderId { get => merchantOrderId; set => merchantOrderId = value; }
        public string TradeTime { get => tradeTime; set => tradeTime = value; }
        public double TradeAmount { get => tradeAmount; set => tradeAmount = value; }
        public double GoodsTotalAmount { get => goodsTotalAmount; set => goodsTotalAmount = value; }
        public string ConsigneeName { get => consigneeName; set => consigneeName = value; }
        public string ConsigneeMobile { get => consigneeMobile; set => consigneeMobile = value; }
        public string AddrCountry { get => addrCountry; set => addrCountry = value; }
        public string AddrProvince { get => addrProvince; set => addrProvince = value; }
        public string AddrCity { get => addrCity; set => addrCity = value; }
        public string AddrDistrict { get => addrDistrict; set => addrDistrict = value; }
        public string AddrDetail { get => addrDetail; set => addrDetail = value; }
        public string ZipCode { get => zipCode; set => zipCode = value; }
        public string IdType { get => idType; set => idType = value; }
        public string IdNumber { get => idNumber; set => idNumber = value; }
        public string IdFountImgUrl { get => idFountImgUrl; set => idFountImgUrl = value; }
        public string IdBackImgUrl { get => idBackImgUrl; set => idBackImgUrl = value; }
        public string Status { get => status; set => status = value; }
        public string PurchaserCode { get => purchaserCode; set => purchaserCode = value; }
        public string DistributionCode { get => distributionCode; set => distributionCode = value; }
        public string Apitype { get => apitype; set => apitype = value; }
        public string FqID { get => fqID; set => fqID = value; }
        public string Sendapi { get => sendapi; set => sendapi = value; }
        public string Freight { get => freight; set => freight = value; }
        public List<GoodsBean> GoodsList { get => goodsList; set => goodsList = value; }
        public string Id { get => id; set => id = value; }
    }
    class GoodsBean
    {
        private string id;
        private string merchantOrderId;
        private string barCode;
        private double skuUnitPrice;
        private int quantity;
        private string goodsName;
        private string sendType;
        private string skubillname;
        private double supplyPrice;
        private double purchasePrice;
        private string wid;//存放获取仓库商品库存的id

        public GoodsBean(string id, string merchantOrderId, string barCode, double skuUnitPrice, int quantity, string goodsName, string sendType, string skubillname, double supplyPrice, double purchasePrice)
        {
            this.id = id;
            this.merchantOrderId = merchantOrderId;
            this.barCode = barCode;
            this.skuUnitPrice = skuUnitPrice;
            this.quantity = quantity;
            this.goodsName = goodsName;
            this.sendType = sendType;
            this.skubillname = skubillname;
            this.supplyPrice = supplyPrice;
            this.purchasePrice = purchasePrice;
        }

        public string MerchantOrderId { get => merchantOrderId; set => merchantOrderId = value; }
        public string BarCode { get => barCode; set => barCode = value; }
        public double SkuUnitPrice { get => skuUnitPrice; set => skuUnitPrice = value; }
        public int Quantity { get => quantity; set => quantity = value; }
        public string GoodsName { get => goodsName; set => goodsName = value; }
        public string SendType { get => sendType; set => sendType = value; }
        public string Skubillname { get => skubillname; set => skubillname = value; }
        public double SupplyPrice { get => supplyPrice; set => supplyPrice = value; }
        public double PurchasePrice { get => purchasePrice; set => purchasePrice = value; }
        public string Id { get => id; set => id = value; }
        public string Wid { get => wid; set => wid = value; }
    }
    public class OrderItem
    {
        public string keyId;//序号
        public string id;
        public string status;//状态
        public string ifSend;//是否有发货按钮0没有1有
        public string warehouseId;//仓库id
        public string warehouseCode;//仓库code
        public string warehouseName;//仓库名
        public string parentOrderId;//父订单号
        public string merchantOrderId;//订单号
        public string tradeTime;//订单时间
        public string expressName;//快递公司
        public string waybillno;//运单号
        public string purchase;//渠道商
        public string purchaseId;//渠道商id
        public string supplier;//供应商
        public string supplierAgentCode;//供应代理usercode
        public string purchaseAgentCode;//采购代理usercode
        public string consigneeCode;//收货人的账号
        public string consigneeName;//收货人
        public string tradeAmount;//订单总金额
        public string idNumber;//身份证号
        public string consigneeMobile;//收货人电话
        public string addrCountry;//国家
        public string addrProvince;//省份
        public string addrCity;//城市
        public string addrDistrict;//县区
        public string addrDetail;//详细地址
        public double freight;//运费
        public string platformId;//平台渠道id
        public double sales;//销量
        public double purchaseTotal;//渠道利润
        public double agentTotal;//代理利润
        public double dealerTotal;//分销利润
        public string distribution;//分销商
        public string consignorName;//发货人
        public string consignorMobile;//发货人电话
        public string consignorAddr;//发货人地址
        public string payType;//支付类型
        public string payNo;//支付单号
        public string payTime;//支付单生成时间

        public List<OrderGoodsItem> OrderGoods;//商品列表
    }
    public class OrderGoodsItem
    { public string id;
        public string slt;//商品图片
        public string barCode;//条码
        public double skuUnitPrice;//销售单价
        public double totalPrice;//销售总价
        public string skuBillName;//名称
        public double quantity;//数量
        public double purchasePrice;//供应价
        public string suppliercode;//供应商code
        public double supplyPrice;//进价
        public double tax;//税
        public double waybillPrice;//运费
        public double platformPrice;//平台提点
        public double supplierAgentPrice;//供货代理提点
        public string supplierAgentCode;//供应代理usercode
        public double purchaseAgentPrice;//采购代理提点
        public string purchaseAgentCode;//采购代理usercode
        public double profitPlatform;//平台利润
        public double profitAgent;//代理利润
        public double profitDealer;//分销利润
        public double profitOther1;//其他利润1
        public string other1Name;//其他1名称
        public double profitOther2;//其他利润2
        public string other2Name;//其他2名称
        public double profitOther3;//其他利润3
        public string other3Name;//其他3名称
        public double purchaseP;//渠道利润
        public DataRow dr;
    }
}
