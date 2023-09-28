﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.ObjectModel;
using System.Text;
using System.Web.UI.HtmlControls;

using Microsoft.VisualBasic;
using Infragistics.UltraChart.Shared.Styles;
using Infragistics.UltraChart.Core.Layers;
using Infragistics.UltraChart.Resources.Appearance;
using System.IO;
using System.Drawing;
using Infragistics.UltraChart.Data.Series;
using Infragistics.UltraChart.Shared.Events;
using LiveMonitoring;
using System.Diagnostics;

namespace website2016V2.Metering
{
    public partial class MeteringBilling : System.Web.UI.Page
    {
        private Infragistics.WebUI.UltraWebChart.UltraChart MyDateLinechart1 = new Infragistics.WebUI.UltraWebChart.UltraChart();
        private Infragistics.WebUI.UltraWebChart.UltraChart MyDateLinechart2 = new Infragistics.WebUI.UltraWebChart.UltraChart();
        private bool GeneratePDF = false;
        private Collection Rep2Images = new Collection();
        private StringBuilder ExportStr = new StringBuilder();
        private StringBuilder Rep2HTML = new StringBuilder();
        private LiveMonitoring.DataAccess MyDataAccess = new LiveMonitoring.DataAccess();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (((string)Session["LoggedIn"] == "True"))
            {
                LiveMonitoring.IRemoteLib.UserDetails MyUser = new LiveMonitoring.IRemoteLib.UserDetails();
                MyUser = (LiveMonitoring.IRemoteLib.UserDetails)Session["UserDetails"];
                Label user = this.Master.FindControl("lblUser") as Label;
                Label loginUser = this.Master.FindControl("lblUser2") as Label;
                Label LastLogin = this.Master.FindControl("LastLogin") as Label;
                loginUser.Text = ((MyUser.FirstName + (" " + MyUser.SurName)));
                user.Text = ((MyUser.FirstName + (" " + MyUser.SurName)));
                LastLogin.Text = " LL:" + MyUser.LoginDT.ToString();
                DropDownList sites = this.Master.FindControl("cmbCurrentSite") as DropDownList;
                sites.Visible = false;
                string name = System.IO.Path.GetFileName(Request.ServerVariables["SCRIPT_NAME"]).Replace(".aspx", "");
                //LiveMonitoring.IRemoteLib.UserDetails MyUser = new LiveMonitoring.IRemoteLib.UserDetails();
                // LiveMonitoring.PageSecurityClass IPMonPageSecure = new LiveMonitoring.PageSecurityClass();
                LiveMonitoring.PageSecurityClass IPMonPageSecure = new LiveMonitoring.PageSecurityClass();
                int MyIPMonPageSecure = IPMonPageSecure.get_GetViewLevelByName(name);
                MyUser = (LiveMonitoring.IRemoteLib.UserDetails)Session["UserDetails"];
                if (MyIPMonPageSecure > MyUser.UserLevel)
                {
                    Response.Redirect("NotAuthorisedView.aspx");
                }
                

                if(IsPostBack == false)
                {
                    try
                    {
                        if (Convert.ToBoolean(MyDataAccess.GetAppSetting("BySite")) == true)
                        {
                            if (MyUser.UserSites.Count > 1)
                            {
                                cmbCurrentSite.Visible = true;
                                cmbCurrentSite.BorderColor = System.Drawing.Color.SeaGreen;
                            }
                            cmbCurrentSite.Items.Clear();
                            Sites RetSites = new Sites(MyUser.ID);

                            List<MySite> MySitesList = new List<MySite>();
                            bool firstitem = true;
                            foreach (Sites.Site MySiteID in RetSites.SitesList)
                            {
                                try
                                {
                                    System.Web.UI.WebControls.ListItem MyItem = new System.Web.UI.WebControls.ListItem();
                                    MyItem.Text = MySiteID.SiteObj.SiteName;
                                    //MySiteID.SiteName
                                    MyItem.Value = MySiteID.SiteObj.SiteID.ToString();

                                    if ((Session["SelectedSite"] == null) == false)
                                    {
                                        if (Convert.ToInt32(Session["SelectedSite"]) == MySiteID.SiteObj.SiteID)
                                        {
                                            MyItem.Selected = true;
                                        }
                                    }
                                    else
                                    {
                                        if (firstitem)
                                        {
                                            Session["SelectedSite"] = MySiteID.SiteObj.SiteID;
                                            MyItem.Selected = true;
                                            firstitem = false;
                                        }
                                    }
                                    cmbCurrentSite.Items.Add(MyItem);
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                            try
                            {
                                LiveMonitoring.testing test = new LiveMonitoring.testing();
                                test.SortDropDown(cmbCurrentSite);
                            }
                            catch (Exception ex)
                            {
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                    }

                    Response.Expires = 5;
                    Page.MaintainScrollPositionOnPostBack = true;
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Minute, -30, DateTime.Now);
                    Session["EndDate"] = DateTime.Now;

                    int MySensorNum = 0;
                    MySensorNum = Convert.ToInt32(Request.QueryString["SensorNum"]);
                    Load_Sensors(MySensorNum);
                    Load_Tarrifs();
                    Session["Sensors"] = "";
                    LiveMonitoring.GlobalRemoteVars MyRem = new LiveMonitoring.GlobalRemoteVars();

                    if (MySensorNum == 0)
                    {

                    }
                    else
                    {
                        Collection MyCollection = new Collection();
                        MyCollection = MyRem.get_GetServerObjects(((Session["SelectedSite"] == null) ? null : Session["SelectedSite"]));
                        //MyCollection = MyRem.get_GetServerObjects(); //get_GetServerObjects 'server1.GetAll();
                        object MyObject1 = null;
                        int MyCnt = 0;
                        foreach (object MyObject1_loopVariable in MyCollection)
                        {
                            MyObject1 = MyObject1_loopVariable;
                            if (MyObject1 is LiveMonitoring.IRemoteLib.SensorDetails)
                            {
                                LiveMonitoring.IRemoteLib.SensorDetails MySensor = (LiveMonitoring.IRemoteLib.SensorDetails)MyObject1;
                                if (MySensorNum == MySensor.ID)
                                {
                                    Session["Sensors"] += MySensor.ID.ToString() + ",";
                                    AddLayer(MySensor);
                                    break; // TODO: might not be correct. Was : Exit For
                                }
                                else
                                {
                                    MyCnt += 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void cmbCurrentSite_SelectedIndexChanged(object sender, EventArgs e)
        {
            Session["SelectedSite"] = cmbCurrentSite.SelectedValue;
        }

        public void AddPageBreak()
        {
            HtmlGenericControl MyHtml = new HtmlGenericControl();
            MyHtml.InnerHtml = "<div style=\"height:1px\">&nbsp;</div><div style=\"page-break-before: always; height:1px;\">&nbsp;</div>";
            this.Charts.Controls.Add(MyHtml);


            // MyHtml.InnerHtml = "<tr style=""page-break-before: always;"">"
            //this.Charts.Controls.Add(MyHtml);
        }

        private void Load_Tarrifs()
        {
            LiveMonitoring.GlobalRemoteVars MyRem = new LiveMonitoring.GlobalRemoteVars();
            List<LiveMonitoring.IRemoteLib.MeteringTariff> MyNewList = MyRem.LiveMonServer.GetMeteringTarrifNames();
            bool Firstone = true;
            foreach (LiveMonitoring.IRemoteLib.MeteringTariff MyTarrif in MyNewList)
            {
                System.Web.UI.WebControls.ListItem MyIttem = new System.Web.UI.WebControls.ListItem();
                MyIttem.Text = MyTarrif.TarriffName;
                MyIttem.Value = MyTarrif.ID.ToString();
                ddlTarrif.Items.Add(MyIttem);
                if (Firstone)
                {
                    MyIttem.Selected = true;
                    Firstone = false;
                }
            }
        }

        public void Load_Sensors(int SelectedID)
        {
            Collection MyCollection = new Collection();
            LiveMonitoring.GlobalRemoteVars MyRem = new LiveMonitoring.GlobalRemoteVars();
            MyCollection = MyRem.get_GetServerObjects(((Session["SelectedSite"] == null) ? null : Session["SelectedSite"]));
            //GetServerObjects 'server1.GetAll()
            object MyObject1 = null;
            bool added = false;
            Array ItemValue = System.Enum.GetValues(typeof(LiveMonitoring.IRemoteLib.SensorDetails.SensorType));
            Array Item = System.Enum.GetNames(typeof(LiveMonitoring.IRemoteLib.SensorDetails.SensorType));
            bool Firstone = true;


            foreach (object MyObject1_loopVariable in MyCollection)
            {
                MyObject1 = MyObject1_loopVariable;
                if (MyObject1 is LiveMonitoring.IRemoteLib.SensorDetails)
                {
                    LiveMonitoring.IRemoteLib.SensorDetails MySensor = (LiveMonitoring.IRemoteLib.SensorDetails)MyObject1;
                    //only meters for selected Site
                    // If MySensor.SiteID = CInt(Session("Site")) Then
                    if (MySensor.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1700MeterReadProfile | 
                        MySensor.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1140MeterReadProfile | 
                        MySensor.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.RockwellPM1000EnergyLogResults |
                        MySensor.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.LandisGyrE650Profile)
                    {
                        System.Web.UI.WebControls.ListItem MyIttem = new System.Web.UI.WebControls.ListItem();
                        MyIttem.Text = MySensor.Caption;
                        MyIttem.Value = MySensor.ID.ToString();
                        ddlMeters.Items.Add(MyIttem);
                        if (Firstone)
                        {
                            MyIttem.Selected = true;
                            Firstone = false;
                        }
                    }
                }
            }
        }

        public void AddTarrifReport(LiveMonitoring.IRemoteLib.SensorDetails SensorDet)
        {
            //MyCollection
            List<LiveMonitoring.IRemoteLib.MeteringProfileRecord> MyData = null;
            List<LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord> MyDataHist = null;
            LiveMonitoring.IRemoteLib.MeteringTarrifDetails MyTarrif = default(LiveMonitoring.IRemoteLib.MeteringTarrifDetails);
            List<LiveMonitoring.IRemoteLib.MeteringProfileMarker> MyDataMarkers = null;
            LiveMonitoring.GlobalRemoteVars MyRem = new LiveMonitoring.GlobalRemoteVars();
            try
            {
                if (Information.IsDate(Session["StartDate"]) == true & Information.IsDate(Session["EndDate"]) == true)
                {
                    //If cmbDataSet.SelectedValue = 0 Then
                    if (SensorDet.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1140MeterReadProfile | SensorDet.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1700MeterReadProfile)
                    {
                        try
                        {
                            MyData = MyRem.LiveMonServer.GetMeteringProfileRecord(SensorDet.ID, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));

                        }
                        catch (Exception ex)
                        {
                        }
                        try
                        {
                            MyDataMarkers = MyRem.LiveMonServer.GetMeteringProfileMarkers(SensorDet.ID, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));

                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    if (SensorDet.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.RockwellPM1000EnergyLogResults| SensorDet.Type == IRemoteLib.SensorDetails.SensorType.LandisGyrE650Profile)
                    {
                        try
                        {
                            MyDataHist = MyRem.LiveMonServer.GetMeteringDataHistoryRecord(SensorDet.ID, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));

                        }
                        catch (Exception ex)
                        {
                        }
                    }


                    //Else
                    //MyData = MyRem.server1.GetFilteredSensorHistory(cmbDataSet.SelectedValue, SensorDet.ID, CDate(Session("StartDate")), CDate(Session("EndDate")))
                    // End If
                    try
                    {
                        //GetMeteringTarrifEvent
                        MyTarrif = MyRem.LiveMonServer.GetMeteringTarrif(Convert.ToInt32(ddlTarrif.SelectedValue));

                    }
                    catch (Exception ex)
                    {
                    }
                }

            }
            catch (Exception ex)
            {
            }

            switch (SensorDet.Type)
            {
                case LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1140MeterReadProfile:
                    if ((MyData == null) == false & (MyTarrif == null) == false)
                    {
                        DrawElsterA1140TarrifReport(SensorDet, MyData, MyDataMarkers, MyTarrif);
                    }
                    break;
                case LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1700MeterReadProfile:
                    if ((MyData == null) == false & (MyTarrif == null) == false)
                    {
                        DrawElsterA1700TarrifReport(SensorDet, MyData, MyDataMarkers, MyTarrif);
                    }
                    break;
                case LiveMonitoring.IRemoteLib.SensorDetails.SensorType.RockwellPM1000EnergyLogResults:
                    if ((MyDataHist == null) == false & (MyTarrif == null) == false)
                    {
                        DrawRockwellPM1000EnergyLogReport(SensorDet, MyDataHist, MyTarrif);
                    }

                    break;
                case LiveMonitoring.IRemoteLib.SensorDetails.SensorType.LandisGyrE650Profile:
                    if ((MyDataHist == null) == false & (MyTarrif == null) == false)
                    {
                        DrawLandisLogReport(SensorDet, MyDataHist, MyTarrif);
                    }

                    break;
            }

        }
        public void DrawLandisLogReport(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord> MyDataHist, LiveMonitoring.IRemoteLib.MeteringTarrifDetails MyTarrif)
        {
            this.DivTarrifReport.Controls.Clear();
            try
            {
                StringBuilder MyHtmlStr = new StringBuilder();
                MyHtmlStr.Append("<table class=\"table table-striped responsive-utilities jambo_table bulk_action table-bordered\" BorderColor=\"#316AC5\" border=1><tr><td>");
                MyHtmlStr.Append("Tarrif Report:" + SensorDet.Caption);
                MyHtmlStr.Append("</td></tr><tr><td>");
                MyHtmlStr.Append("Meter :" + SensorDet.Caption);
                //ExcelStrData += "Sensor:" & SensorDet.Caption + vbCrLf
                MyHtmlStr.Append("</td></tr><tr><td>");
                MyHtmlStr.Append("From :" + Session["StartDate"] + " To :" + Session["EndDate"]);
                MyHtmlStr.Append("</td></tr></table>");

                ///''''''''''''''''''
                int Bcnt = 0;
                long MyPeriod = 30;
                //default to 30 minutes
                //check if we have a channel config else use default 69
                MyPeriod = returnRockwellMeteringPeriod(MyDataHist);

                //ok now we have channels set the names


                int tmp1cntwe = 0;
                //mycnt1= fields ?
                //LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyDataHistory = default(LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord);
                LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyPrevDataHistory = default(LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord);
                double TotKWhCnt = 0;
                double TotKWCnt = 0;
                double TotKvarCnt = 0;
                double PeakKWhCnt = 0;
                double StandKWhCnt = 0;
                double OffPeakKWhCnt = 0;
                double PeakKWhCost = 0;
                double StandKWhCost = 0;
                double OffPeakKWhCost = 0;
                double PeakKWhTotCost = 0;
                double StandKWhTotCost = 0;
                double OffPeakKWhTotCost = 0;
                string PeakKWhLabel = "";
                // = 0
                string StandKWhLabel = "";
                // = 0
                string OffPeakKWhLabel = "";
                // = 0

                double MaxKvaDemand = 0;
                System.DateTime MaxKvaDemandDate = default(System.DateTime);
                double MaxKwhDemand = 0;
                System.DateTime MaxKwhDemandDate = default(System.DateTime);
                double periodCnt = 0;
                double KVAVal = 0;
                double kwhVal = 0;

                int MaxFieldCnt = 0;
                bool firstrec = true;
                foreach (LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyDataHistory in MyDataHist)
                {
                    try
                    {
                        if (firstrec)
                        {
                            MyPrevDataHistory = MyDataHistory;
                            firstrec = false;
                        }
                        else
                        {
                            // MyDataHistory.TimeStamp
                            //MyTarrif
                            //MyTarrif.ActiveEnergyCharges()
                            LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges TypePeriod = new LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges();
                            int TmpChargePeriod = 0;
                            //= FindPeriod(MyDataHistory.TimeStamp, MyTarrif.ActiveEnergyCharges, TypePeriod)
                            try
                            {
                                //Dim TypePeriod As New LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges
                                TmpChargePeriod = FindPeriod(MyDataHistory.TimeStamp, MyTarrif.ActiveEnergyCharges, ref TypePeriod);

                            }
                            catch (Exception ex)
                            {
                            }
                            if ((TypePeriod.ChargeName == null) == false & (TmpChargePeriod == 0) == true)
                            {
                                switch (TypePeriod.ChargePeriods[TmpChargePeriod].MeteringChargeType.ID)
                                {
                                    //cant mix peak and offp eak so first one should be correct
                                    case 1:
                                        //peak
                                        PeakKWhCnt += (MyDataHistory.kWhValue );
                                        //* (MyPeriod / 60)
                                        PeakKWhCost = TypePeriod.CostcPerKWh;
                                        PeakKWhLabel = TypePeriod.ChargeName;
                                        break;
                                    case 2:
                                        //standard
                                        StandKWhCnt += (MyDataHistory.kWhValue );
                                        //* (MyPeriod / 60)
                                        StandKWhCost = TypePeriod.CostcPerKWh;
                                        StandKWhLabel = TypePeriod.ChargeName;
                                        break;
                                    case 3:
                                        //off peak
                                        OffPeakKWhCnt += (MyDataHistory.kWhValue);
                                        //* (MyPeriod / 60)
                                        OffPeakKWhCost = TypePeriod.CostcPerKWh;
                                        OffPeakKWhLabel = TypePeriod.ChargeName;
                                        break;
                                }
                                //standard kwh by default
                            }
                            else
                            {
                                StandKWhCnt += (MyDataHistory.kWhValue );
                                //* (MyPeriod / 60)
                                StandKWhCost = TypePeriod.CostcPerKWh;
                                StandKWhLabel = TypePeriod.ChargeName;
                            }

                            TotKWhCnt += (MyDataHistory.kWhValue );
                            //* (MyPeriod / 60)
                            TotKWCnt += (MyDataHistory.kWhValue ) * (60 / MyPeriod);
                            kwhVal = (MyDataHistory.kWhValue );
                            //* (MyPeriod / 60)
                            //KVA findMVA
                            TotKvarCnt += (MyDataHistory.kVARValue );
                            KVAVal = (MyDataHistory.kVAValue );

                            periodCnt += 1;
                            //0 throws out calculations
                            if (MyPrevDataHistory.kWhValue > 0)
                            {
                                if (kwhVal > MaxKwhDemand)
                                {
                                    MaxKwhDemand = kwhVal;
                                    MaxKwhDemandDate = MyDataHistory.TimeStamp;
                                }
                                if (KVAVal > MaxKvaDemand)
                                {
                                    MaxKvaDemand = KVAVal;
                                    MaxKvaDemandDate = MyDataHistory.TimeStamp;
                                }
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                    }
                    MyPrevDataHistory = MyDataHistory;
                    // End If
                }
                try
                {
                    MyHtmlStr.Append("<table class=\"table table-striped responsive-utilities jambo_table bulk_action table-bordered\" BorderColor=\"#316AC5\" border=1 width=450 ><tr><td>");
                    MyHtmlStr.Append("Tarrif  :" + MyTarrif.TarrifDetails.TarriffName);
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Metering Interval  :" + MyPeriod.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");

                    MyHtmlStr.Append("Max Kwh</td><td>" + MaxKwhDemand.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kwh Date </td><td>" + MaxKwhDemandDate.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kva</td><td>" + MaxKvaDemand.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kva Date </td><td>" + MaxKvaDemandDate.ToString() + "</td></tr><tr><td>");

                    MyHtmlStr.Append("Total Kwh:" + TotKWhCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Total KW :" + TotKWCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Total KVA :" + TotKvarCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Billing :");
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    if (PeakKWhCost > 0)
                    {
                        if (PeakKWhCnt > 0)
                        {
                            MyHtmlStr.Append(PeakKWhLabel + "  " + PeakKWhCnt.ToString("#.00") + " KWh @ " + (PeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R" + ((PeakKWhCost * PeakKWhCnt) / 100).ToString("#.00"));
                            PeakKWhTotCost = (PeakKWhCost * PeakKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(PeakKWhLabel + "  " + PeakKWhCnt.ToString("#.00") + " KWh @ " + (PeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R0");
                            PeakKWhTotCost = 0;
                            //(PeakKWhCost * PeakKWhCnt) / 100
                        }
                        MyHtmlStr.Append("</td></tr><tr><td>");
                    }

                    if (StandKWhCost > 0)
                    {
                        if (StandKWhCnt > 0)
                        {
                            MyHtmlStr.Append(StandKWhLabel + "  " + StandKWhCnt.ToString("#.00") + " KWh @ " + (StandKWhCost / 100).ToString() + " R/KWh </td><td>   R" + ((StandKWhCost * StandKWhCnt) / 100).ToString("#.00"));
                            StandKWhTotCost = (StandKWhCost * StandKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(StandKWhLabel + "  " + StandKWhCnt.ToString("#.00") + " KWh @ " + (StandKWhCost / 100).ToString() + " R/KWh  </td><td>  R0");
                            StandKWhTotCost = 0;
                        }
                        MyHtmlStr.Append("</td></tr><tr><td>");
                    }

                    if (OffPeakKWhCost > 0)
                    {
                        if (OffPeakKWhCnt > 0)
                        {
                            MyHtmlStr.Append(OffPeakKWhLabel + "  " + OffPeakKWhCnt.ToString("#.00") + " KWh @ " + (OffPeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R" + ((OffPeakKWhCost * OffPeakKWhCnt) / 100).ToString("#.00"));
                            OffPeakKWhTotCost = (OffPeakKWhCost * OffPeakKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(OffPeakKWhLabel + "  " + OffPeakKWhCnt.ToString("#.00") + " KWh @ " + (OffPeakKWhCost / 100).ToString() + " R/KWh   </td><td> R0");
                            OffPeakKWhTotCost = 0;
                        }
                    }

                    double totCharge = 0;
                    try
                    {
                        double[] ChargeVals = new double[Information.UBound(MyTarrif.MeteringNetworkCharge)];
                        LiveMonitoring.IRemoteLib.MeteringNetworkCharges MyNetCharge = default(LiveMonitoring.IRemoteLib.MeteringNetworkCharges);
                        for (int myCnt = 0; myCnt <= Information.UBound(MyTarrif.MeteringNetworkCharge) - 1; myCnt++)
                        {
                            MyNetCharge = MyTarrif.MeteringNetworkCharge[myCnt];
                            if ((MyNetCharge == null) == false)
                            {
                                if (MyNetCharge.Percentage > 0)
                                {
                                    //calculate percentage
                                    double TotColVal = 0;
                                    string Cols = "";
                                    try
                                    {
                                        foreach (int MyColumns in MyNetCharge.Columns)
                                        {
                                            TotColVal += (ChargeVals[MyColumns] * (MyNetCharge.Percentage / 100));
                                            Cols += MyColumns.ToString() + ",";
                                        }
                                        Cols = Cols.Remove(Cols.LastIndexOf(","), 1);
                                        TotColVal += ((PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * (MyNetCharge.Percentage / 100));

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  (" + Cols + ")  @ " + (MyNetCharge.Percentage).ToString() + " %  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));

                                }
                                //If MyNetCharge.CostRperkVA > 0 Then
                                //    'calculate CostRperkVA
                                //    Dim TotColVal As Double = 0
                                //    Try
                                //        TotColVal = TotKvarCnt * MyNetCharge.CostRperkVA
                                //    Catch ex As Exception

                                //    End Try
                                //    ChargeVals(myCnt) = TotColVal
                                //    MyHtmlStr.Append("</td></tr><tr><td>")
                                //    MyHtmlStr.Append(MyNetCharge.ChargeName + "  " + TotKvarCnt.ToString("#.00") + "kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                //End If
                                if (MyNetCharge.CostRperkVA > 0)
                                {
                                    //calculate CostRperkVA
                                    //check for NMD value
                                    double TotColVal = 0;
                                    if (MyNetCharge.MaximumDemand > 0)
                                    {
                                        //ok bellow notified
                                        //SensorDet.ExtraValue1
                                        double CheckDemand = 0;
                                        //CheckDemand = SensorDet.ExtraValue1;
                                        DataManager myDataManager = new DataManager();
                                        CheckDemand = myDataManager.getMeterNMD(SensorDet.ID, DateTime.Now.Month, DateTime.Now.Year);
                                        if (MaxKvaDemand <= CheckDemand)
                                        {
                                            try
                                            {
                                                TotColVal = CheckDemand * MyNetCharge.CostRperkVA;

                                            }
                                            catch (Exception ex)
                                            {
                                            }
                                            ChargeVals[myCnt] = TotColVal;
                                            MyHtmlStr.Append("</td></tr><tr><td>");
                                            MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                        }
                                        else
                                        {
                                            //pay penalty above notified
                                            try
                                            {
                                                TotColVal = (CheckDemand * MyNetCharge.CostRperkVA) + (MaxKvaDemand - CheckDemand) * MyNetCharge.PenaltyCharge;

                                            }
                                            catch (Exception ex)
                                            {
                                            }
                                            ChargeVals[myCnt] = TotColVal;
                                            MyHtmlStr.Append("</td></tr><tr><td>");
                                            // MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  PEN: R" + MyNetCharge.PenaltyCharge.ToString + " PEN KVA:" + (TotKvarCnt - CheckDemand).ToString("#.00") + "  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                            // MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  NB:Above NMD  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                            MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA    </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                        }


                                    }
                                    else
                                    {
                                        try
                                        {
                                            TotColVal = MaxKvaDemand * MyNetCharge.CostRperkVA;

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        ChargeVals[myCnt] = TotColVal;
                                        MyHtmlStr.Append("</td></tr><tr><td>");
                                        MyHtmlStr.Append(MyNetCharge.ChargeName + "  " + MaxKvaDemand.ToString("#.00") + "kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                    }
                                }
                                if (MyNetCharge.FixedCost > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = MyNetCharge.FixedCost;
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.FixedCost).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperkWh > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    try
                                    {
                                        TotColVal = TotKWhCnt * MyNetCharge.CostRperkWh;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperkWh).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperMaxkVA > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    try
                                    {
                                        TotColVal = MaxKvaDemand * MyNetCharge.CostRperMaxkVA;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.CostRperMaxkVA
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperMaxkVA).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperday > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    long DayCnt = DateAndTime.DateDiff(DateInterval.Day, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));
                                    try
                                    {
                                        TotColVal = DayCnt * MyNetCharge.CostRperday;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.CostRperday
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperday).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                            }
                            totCharge += ChargeVals[myCnt];
                        }

                    }
                    catch (Exception ex)
                    {
                    }
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Subtotal  " + "  </td><td>  R" + (totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost).ToString("#.00"));
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Vat @ 15%  " + "  </td><td>  R" + ((totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * 0.15).ToString("#.00"));
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Total  " + "  </td><td>  R" + (totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost + ((totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * 0.14)).ToString("#.00"));

                    MyHtmlStr.Append("</td></tr></table>");

                }
                catch (Exception ex)
                {
                }


                this.DivTarrifReport.InnerHtml = MyHtmlStr.ToString();
                if (GeneratePDF)
                {
                    Rep2HTML.Append(MyHtmlStr);
                }
                AddPageBreak();


            }
            catch (Exception ex)
            {
            }


        }

        public void DrawRockwellPM1000EnergyLogReport(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord> MyDataHist, LiveMonitoring.IRemoteLib.MeteringTarrifDetails MyTarrif)
        {
            this.DivTarrifReport.Controls.Clear();
            try
            {
                StringBuilder MyHtmlStr = new StringBuilder();
                MyHtmlStr.Append("<table class=\"table table-striped responsive-utilities jambo_table bulk_action table-bordered\" BorderColor=\"#316AC5\" border=1><tr><td>");
                MyHtmlStr.Append("Tarrif Report:" + SensorDet.Caption);
                MyHtmlStr.Append("</td></tr><tr><td>");
                MyHtmlStr.Append("Meter :" + SensorDet.Caption);
                //ExcelStrData += "Sensor:" & SensorDet.Caption + vbCrLf
                MyHtmlStr.Append("</td></tr><tr><td>");
                MyHtmlStr.Append("From :" + Session["StartDate"] + " To :" + Session["EndDate"]);
                MyHtmlStr.Append("</td></tr></table>");

                ///''''''''''''''''''
                int Bcnt = 0;
                long MyPeriod = 15;
                //default to 30 minutes
                //check if we have a channel config else use default 69
                MyPeriod = returnRockwellMeteringPeriod(MyDataHist);

                //ok now we have channels set the names


                int tmp1cntwe = 0;
                //mycnt1= fields ?
                //LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyDataHistory = default(LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord);
                LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyPrevDataHistory = default(LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord);
                double TotKWhCnt = 0;
                double TotKWCnt = 0;
                double TotKvarCnt = 0;
                double PeakKWhCnt = 0;
                double StandKWhCnt = 0;
                double OffPeakKWhCnt = 0;
                double PeakKWhCost = 0;
                double StandKWhCost = 0;
                double OffPeakKWhCost = 0;
                double PeakKWhTotCost = 0;
                double StandKWhTotCost = 0;
                double OffPeakKWhTotCost = 0;
                string PeakKWhLabel = "";
                // = 0
                string StandKWhLabel = "";
                // = 0
                string OffPeakKWhLabel = "";
                // = 0

                double MaxKvaDemand = 0;
                System.DateTime MaxKvaDemandDate = default(System.DateTime);
                double MaxKwhDemand = 0;
                System.DateTime MaxKwhDemandDate = default(System.DateTime);
                double periodCnt = 0;
                double KVAVal = 0;
                double kwhVal = 0;

                int MaxFieldCnt = 0;
                bool firstrec = true;
                foreach (LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyDataHistory in MyDataHist)
                {
                    try
                    {
                        if (firstrec)
                        {
                            MyPrevDataHistory = MyDataHistory;
                            firstrec = false;
                        }
                        else
                        {
                            // MyDataHistory.TimeStamp
                            //MyTarrif
                            //MyTarrif.ActiveEnergyCharges()
                            LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges TypePeriod = new LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges();
                            int TmpChargePeriod = 0;
                            //= FindPeriod(MyDataHistory.TimeStamp, MyTarrif.ActiveEnergyCharges, TypePeriod)
                            try
                            {
                                //Dim TypePeriod As New LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges
                                TmpChargePeriod = FindPeriod(MyDataHistory.TimeStamp, MyTarrif.ActiveEnergyCharges, ref TypePeriod);

                            }
                            catch (Exception ex)
                            {
                            }
                            if ((TypePeriod.ChargeName == null) == false & (TmpChargePeriod == 0) == true)
                            {
                                switch (TypePeriod.ChargePeriods[TmpChargePeriod].MeteringChargeType.ID)
                                {
                                    //cant mix peak and offp eak so first one should be correct
                                    case 1:
                                        //peak
                                        PeakKWhCnt += (MyDataHistory.kWhValue - MyPrevDataHistory.kWhValue);
                                        //* (MyPeriod / 60)
                                        PeakKWhCost = TypePeriod.CostcPerKWh;
                                        PeakKWhLabel = TypePeriod.ChargeName;
                                        break;
                                    case 2:
                                        //standard
                                        StandKWhCnt += (MyDataHistory.kWhValue - MyPrevDataHistory.kWhValue);
                                        //* (MyPeriod / 60)
                                        StandKWhCost = TypePeriod.CostcPerKWh;
                                        StandKWhLabel = TypePeriod.ChargeName;
                                        break;
                                    case 3:
                                        //off peak
                                        OffPeakKWhCnt += (MyDataHistory.kWhValue - MyPrevDataHistory.kWhValue);
                                        //* (MyPeriod / 60)
                                        OffPeakKWhCost = TypePeriod.CostcPerKWh;
                                        OffPeakKWhLabel = TypePeriod.ChargeName;
                                        break;
                                }
                                //standard kwh by default
                            }
                            else
                            {
                                StandKWhCnt += (MyDataHistory.kWhValue - MyPrevDataHistory.kWhValue);
                                //* (MyPeriod / 60)
                                StandKWhCost = TypePeriod.CostcPerKWh;
                                StandKWhLabel = TypePeriod.ChargeName;
                            }

                            TotKWhCnt += (MyDataHistory.kWhValue - MyPrevDataHistory.kWhValue);
                            //* (MyPeriod / 60)
                            TotKWCnt += (MyDataHistory.kWhValue - MyPrevDataHistory.kWhValue) * (60 / MyPeriod);
                            kwhVal = (MyDataHistory.kWhValue - MyPrevDataHistory.kWhValue);
                            //* (MyPeriod / 60)
                            //KVA findMVA
                            TotKvarCnt += (MyDataHistory.kVARValue - MyPrevDataHistory.kVARValue);
                            KVAVal = (MyDataHistory.kVAValue - MyPrevDataHistory.kVAValue);

                            periodCnt += 1;
                            //0 throws out calculations
                            if (MyPrevDataHistory.kWhValue > 0)
                            {
                                if (kwhVal > MaxKwhDemand)
                                {
                                    MaxKwhDemand = kwhVal;
                                    MaxKwhDemandDate = MyDataHistory.TimeStamp;
                                }
                                if (KVAVal > MaxKvaDemand)
                                {
                                    MaxKvaDemand = KVAVal;
                                    MaxKvaDemandDate = MyDataHistory.TimeStamp;
                                }
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                    }
                    MyPrevDataHistory = MyDataHistory;
                    // End If
                }
                try
                {
                    MyHtmlStr.Append("<table class=\"table table-striped responsive-utilities jambo_table bulk_action table-bordered\" BorderColor=\"#316AC5\" border=1 width=450 ><tr><td>");
                    MyHtmlStr.Append("Tarrif  :" + MyTarrif.TarrifDetails.TarriffName);
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Metering Interval  :" + MyPeriod.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");

                    MyHtmlStr.Append("Max Kwh</td><td>" + MaxKwhDemand.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kwh Date </td><td>" + MaxKwhDemandDate.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kva</td><td>" + MaxKvaDemand.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kva Date </td><td>" + MaxKvaDemandDate.ToString() + "</td></tr><tr><td>");

                    MyHtmlStr.Append("Total Kwh:" + TotKWhCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Total KW :" + TotKWCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Total KVA :" + TotKvarCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Billing :");
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    if (PeakKWhCost > 0)
                    {
                        if (PeakKWhCnt > 0)
                        {
                            MyHtmlStr.Append(PeakKWhLabel + "  " + PeakKWhCnt.ToString("#.00") + " KWh @ " + (PeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R" + ((PeakKWhCost * PeakKWhCnt) / 100).ToString("#.00"));
                            PeakKWhTotCost = (PeakKWhCost * PeakKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(PeakKWhLabel + "  " + PeakKWhCnt.ToString("#.00") + " KWh @ " + (PeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R0");
                            PeakKWhTotCost = 0;
                            //(PeakKWhCost * PeakKWhCnt) / 100
                        }
                        MyHtmlStr.Append("</td></tr><tr><td>");
                    }

                    if (StandKWhCost > 0)
                    {
                        if (StandKWhCnt > 0)
                        {
                            MyHtmlStr.Append(StandKWhLabel + "  " + StandKWhCnt.ToString("#.00") + " KWh @ " + (StandKWhCost / 100).ToString() + " R/KWh </td><td>   R" + ((StandKWhCost * StandKWhCnt) / 100).ToString("#.00"));
                            StandKWhTotCost = (StandKWhCost * StandKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(StandKWhLabel + "  " + StandKWhCnt.ToString("#.00") + " KWh @ " + (StandKWhCost / 100).ToString() + " R/KWh  </td><td>  R0");
                            StandKWhTotCost = 0;
                        }
                        MyHtmlStr.Append("</td></tr><tr><td>");
                    }

                    if (OffPeakKWhCost > 0)
                    {
                        if (OffPeakKWhCnt > 0)
                        {
                            MyHtmlStr.Append(OffPeakKWhLabel + "  " + OffPeakKWhCnt.ToString("#.00") + " KWh @ " + (OffPeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R" + ((OffPeakKWhCost * OffPeakKWhCnt) / 100).ToString("#.00"));
                            OffPeakKWhTotCost = (OffPeakKWhCost * OffPeakKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(OffPeakKWhLabel + "  " + OffPeakKWhCnt.ToString("#.00") + " KWh @ " + (OffPeakKWhCost / 100).ToString() + " R/KWh   </td><td> R0");
                            OffPeakKWhTotCost = 0;
                        }
                    }

                    double totCharge = 0;
                    try
                    {
                        double[] ChargeVals = new double[Information.UBound(MyTarrif.MeteringNetworkCharge)];
                        LiveMonitoring.IRemoteLib.MeteringNetworkCharges MyNetCharge = default(LiveMonitoring.IRemoteLib.MeteringNetworkCharges);
                        for (int myCnt = 0; myCnt <= Information.UBound(MyTarrif.MeteringNetworkCharge) - 1; myCnt++)
                        {
                            MyNetCharge = MyTarrif.MeteringNetworkCharge[myCnt];
                            if ((MyNetCharge == null) == false)
                            {
                                if (MyNetCharge.Percentage > 0)
                                {
                                    //calculate percentage
                                    double TotColVal = 0;
                                    string Cols = "";
                                    try
                                    {
                                        foreach (int MyColumns in MyNetCharge.Columns)
                                        {
                                            TotColVal += (ChargeVals[MyColumns] * (MyNetCharge.Percentage / 100));
                                            Cols += MyColumns.ToString() + ",";
                                        }
                                        Cols = Cols.Remove(Cols.LastIndexOf(","), 1);
                                        TotColVal += ((PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * (MyNetCharge.Percentage / 100));

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  (" + Cols + ")  @ " + (MyNetCharge.Percentage).ToString() + " %  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));

                                }
                                //If MyNetCharge.CostRperkVA > 0 Then
                                //    'calculate CostRperkVA
                                //    Dim TotColVal As Double = 0
                                //    Try
                                //        TotColVal = TotKvarCnt * MyNetCharge.CostRperkVA
                                //    Catch ex As Exception

                                //    End Try
                                //    ChargeVals(myCnt) = TotColVal
                                //    MyHtmlStr.Append("</td></tr><tr><td>")
                                //    MyHtmlStr.Append(MyNetCharge.ChargeName + "  " + TotKvarCnt.ToString("#.00") + "kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                //End If
                                if (MyNetCharge.CostRperkVA > 0)
                                {
                                    //calculate CostRperkVA
                                    //check for NMD value
                                    double TotColVal = 0;
                                    if (MyNetCharge.MaximumDemand > 0)
                                    {
                                        //ok bellow notified
                                        //SensorDet.ExtraValue1
                                        double CheckDemand = 0;
                                        //CheckDemand = SensorDet.ExtraValue1;
                                        DataManager myDataManager = new DataManager();
                                        CheckDemand = myDataManager.getMeterNMD(SensorDet.ID, DateTime.Now.Month, DateTime.Now.Year);

                                        if (MaxKvaDemand <= CheckDemand)
                                        {
                                            try
                                            {
                                                TotColVal = CheckDemand * MyNetCharge.CostRperkVA;

                                            }
                                            catch (Exception ex)
                                            {
                                            }
                                            ChargeVals[myCnt] = TotColVal;
                                            MyHtmlStr.Append("</td></tr><tr><td>");
                                            MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                        }
                                        else
                                        {
                                            //pay penalty above notified
                                            try
                                            {
                                                TotColVal = (CheckDemand * MyNetCharge.CostRperkVA) + (MaxKvaDemand - CheckDemand) * MyNetCharge.PenaltyCharge;

                                            }
                                            catch (Exception ex)
                                            {
                                            }
                                            ChargeVals[myCnt] = TotColVal;
                                            MyHtmlStr.Append("</td></tr><tr><td>");
                                            // MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  PEN: R" + MyNetCharge.PenaltyCharge.ToString + " PEN KVA:" + (TotKvarCnt - CheckDemand).ToString("#.00") + "  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                            // MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  NB:Above NMD  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                            MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA    </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                        }


                                    }
                                    else
                                    {
                                        try
                                        {
                                            TotColVal = MaxKvaDemand * MyNetCharge.CostRperkVA;

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        ChargeVals[myCnt] = TotColVal;
                                        MyHtmlStr.Append("</td></tr><tr><td>");
                                        MyHtmlStr.Append(MyNetCharge.ChargeName + "  " + MaxKvaDemand.ToString("#.00") + "kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                    }
                                }
                                if (MyNetCharge.FixedCost > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = MyNetCharge.FixedCost;
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.FixedCost).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperkWh > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    try
                                    {
                                        TotColVal = TotKWhCnt * MyNetCharge.CostRperkWh;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperkWh).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperMaxkVA > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    try
                                    {
                                        TotColVal = MaxKvaDemand * MyNetCharge.CostRperMaxkVA;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.CostRperMaxkVA
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperMaxkVA).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperday > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    long DayCnt = DateAndTime.DateDiff(DateInterval.Day, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));
                                    try
                                    {
                                        TotColVal = DayCnt * MyNetCharge.CostRperday;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.CostRperday
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperday).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                            }
                            totCharge += ChargeVals[myCnt];
                        }

                    }
                    catch (Exception ex)
                    {
                    }
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Subtotal  " + "  </td><td>  R" + (totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost).ToString("#.00"));
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Vat @ 15%  " + "  </td><td>  R" + ((totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * 0.15).ToString("#.00"));
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Total  " + "  </td><td>  R" + (totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost + ((totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * 0.14)).ToString("#.00"));

                    MyHtmlStr.Append("</td></tr></table>");

                }
                catch (Exception ex)
                {
                }


                this.DivTarrifReport.InnerHtml = MyHtmlStr.ToString();
                if (GeneratePDF)
                {
                    Rep2HTML.Append(MyHtmlStr);
                }
                AddPageBreak();


            }
            catch (Exception ex)
            {
            }


        }
        public long returnRockwellMeteringPeriod(List<LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord> MyDataHist)
        {
            long retval = 15;
           
            //return retval;
            try
            {
                System.DateTime MySdate = MyDataHist[0].TimeStamp;
                System.DateTime MyEdate = MyDataHist[1].TimeStamp;
                 retval = DateAndTime.DateDiff(DateInterval.Minute, MySdate, MyEdate);
          
            }
            catch (Exception ex)
            {
            }
            return retval;
        }

        public void DrawElsterA1140TarrifReport(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringProfileRecord> MyData, List<LiveMonitoring.IRemoteLib.MeteringProfileMarker> MyDataMarkers, LiveMonitoring.IRemoteLib.MeteringTarrifDetails MyTarrif)
        {
            this.DivTarrifReport.Controls.Clear();
            try
            {
                StringBuilder MyHtmlStr = new StringBuilder();
                MyHtmlStr.Append("<table class=\"table table-striped responsive-utilities jambo_table bulk_action table-bordered\" BorderColor=\"#316AC5\" border=1><tr><td>");
                MyHtmlStr.Append("Tarrif Report:" + SensorDet.Caption);
                MyHtmlStr.Append("</td></tr><tr><td>");
                MyHtmlStr.Append("Meter :" + SensorDet.Caption);
                //ExcelStrData += "Sensor:" & SensorDet.Caption + vbCrLf
                //CDate(Session("StartDate")), CDate(Session("EndDate"))
                MyHtmlStr.Append("</td></tr><tr><td>");
                MyHtmlStr.Append("From :" + Session["StartDate"] + " To :" + Session["EndDate"]);
                MyHtmlStr.Append("</td></tr></table>");

                ///''''''''''''''''''
                int Bcnt = 0;
                double MyPeriod = 30;
                //default to 30 minutes
                //check if we have a channel config else use default 69
                int myChannelConfig = 69;
                // LiveMonitoring.IRemoteLib.MeteringProfileMarker MyDataMarkerHistory = default(LiveMonitoring.IRemoteLib.MeteringProfileMarker);
                foreach (LiveMonitoring.IRemoteLib.MeteringProfileMarker MyDataMarkerHistory in MyDataMarkers)
                {
                    if (MyDataMarkerHistory.ProfileMarkerType == 1 | MyDataMarkerHistory.ProfileMarkerType == 4)
                    {
                        myChannelConfig = MyDataMarkerHistory.TRegisterConfigValue;
                        MyPeriod = returnMeteringPeriod(Convert.ToInt32(Conversion.Hex(MyDataMarkerHistory.Period).Substring(0, 1)));
                    }
                }
                //ok now we have channels set the names
                LiveMonitoring.IRemoteLib.ElsterA1140Data.TChannelConfig MyDataChannels = new LiveMonitoring.IRemoteLib.ElsterA1140Data.TChannelConfig(myChannelConfig);
                //Dim mychanel As LiveMonitoring.IRemoteLib.ElsterA1140Data.ChanelData

                //For Each mychanel In MyDataChannels.ChannelNames

                //Next

                double[] ConversionFactor = new double[10];
                int findMVA = 0;
                int findMW = 0;
                for (Bcnt = 0; Bcnt <= 7; Bcnt++)
                {
                    if (MyDataChannels.ChannelNames.Contains((Bcnt).ToString()) == true)
                    {
                        if (((LiveMonitoring.IRemoteLib.ElsterA1140Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelName == "KVA")
                        {
                            findMVA = Bcnt + 1;
                        }
                        if (((LiveMonitoring.IRemoteLib.ElsterA1140Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelName == "Import KW")
                        {
                            findMW = Bcnt + 1;
                        }
                        ConversionFactor[Bcnt] = ((LiveMonitoring.IRemoteLib.ElsterA1140Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelDivisorUnits;
                        //End If
                    }
                    else
                    {
                        ConversionFactor[Bcnt] = 0;
                    }
                }
                //pf possible
                if (MyDataChannels.Import_mW & MyDataChannels.mVA)
                {
                    ConversionFactor[8] = 0;
                }
                int tmp1cntwe = 0;
                //mycnt1= fields ?
                //LiveMonitoring.IRemoteLib.MeteringProfileRecord MyDataHistory = default(LiveMonitoring.IRemoteLib.MeteringProfileRecord);
                double TotKWhCnt = 0;
                double TotKWCnt = 0;
                double TotKvarCnt = 0;
                double PeakKWhCnt = 0;
                double StandKWhCnt = 0;
                double OffPeakKWhCnt = 0;
                double PeakKWhCost = 0;
                double StandKWhCost = 0;
                double OffPeakKWhCost = 0;
                double PeakKWhTotCost = 0;
                double StandKWhTotCost = 0;
                double OffPeakKWhTotCost = 0;
                string PeakKWhLabel = "";
                // = 0
                string StandKWhLabel = "";
                // = 0
                string OffPeakKWhLabel = "";
                // = 0

                double MaxKvaDemand = 0;
                System.DateTime MaxKvaDemandDate = default(System.DateTime);
                double MaxKwhDemand = 0;
                System.DateTime MaxKwhDemandDate = default(System.DateTime);
                double periodCnt = 0;
                double KVAVal = 0;
                double kwhVal = 0;
                int MaxFieldCnt = 0;
                bool firstrec = true;

                foreach (LiveMonitoring.IRemoteLib.MeteringProfileRecord MyDataHistory in MyData)
                {

                    try
                    {
                        // MyDataHistory.TimeStamp
                        //MyTarrif
                        //MyTarrif.ActiveEnergyCharges()
                        LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges TypePeriod = new LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges();
                        int TmpChargePeriod = -1;
                        //= FindPeriod(MyDataHistory.TimeStamp, MyTarrif.ActiveEnergyCharges, TypePeriod)
                        try
                        {
                            //Dim TypePeriod As New LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges
                            TmpChargePeriod = FindPeriod( MyDataHistory.TimeStamp, MyTarrif.ActiveEnergyCharges, ref TypePeriod);


                        }
                        catch (Exception ex)
                        {
                        }
                        if ((TypePeriod.ChargeName == null) == false & (TmpChargePeriod != -1))
                        {
                            switch (TypePeriod.ChargePeriods[TmpChargePeriod].MeteringChargeType.ID)
                            {
                                //cant mix peak and offp eak so first one should be correct
                                case 1:
                                    //peak
                                    PeakKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                                    PeakKWhCost = TypePeriod.CostcPerKWh;
                                    PeakKWhLabel = TypePeriod.ChargeName;
                                    break;
                                case 2:
                                    //standard
                                    StandKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                                    StandKWhCost = TypePeriod.CostcPerKWh;
                                    StandKWhLabel = TypePeriod.ChargeName;
                                    break;
                                case 3:
                                    //off peak
                                    OffPeakKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                                    OffPeakKWhCost = TypePeriod.CostcPerKWh;
                                    OffPeakKWhLabel = TypePeriod.ChargeName;
                                    break;
                            }
                            //standard kwh by default
                        }
                        else
                        {
                            StandKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                            StandKWhCost = TypePeriod.CostcPerKWh;
                            StandKWhLabel = TypePeriod.ChargeName;
                        }


                        //KWH findMW
                        switch (findMW)
                        {
                            case 1:
                                //always 1 import w
                                TotKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                                TotKWCnt += (MyDataHistory.Channel1 / ConversionFactor[0]);
                                break;
                        }
                        kwhVal = (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                        //KVA findMVA
                        switch (findMVA)
                        {
                            case 1:
                                TotKvarCnt += (MyDataHistory.Channel1 / ConversionFactor[0]);
                                KVAVal = (MyDataHistory.Channel1 / ConversionFactor[0]);
                                break;
                            case 2:
                                TotKvarCnt += (MyDataHistory.Channel2 / ConversionFactor[1]);
                                KVAVal = (MyDataHistory.Channel2 / ConversionFactor[1]);
                                break;
                            case 3:
                                TotKvarCnt += (MyDataHistory.Channel3 / ConversionFactor[2]);
                                KVAVal = (MyDataHistory.Channel3 / ConversionFactor[2]);
                                break;
                            case 4:
                                TotKvarCnt += (MyDataHistory.Channel4 / ConversionFactor[3]);
                                KVAVal = (MyDataHistory.Channel4 / ConversionFactor[3]);
                                break;
                            case 5:
                                TotKvarCnt += (MyDataHistory.Channel5 / ConversionFactor[4]);
                                KVAVal = (MyDataHistory.Channel5 / ConversionFactor[4]);
                                break;
                            case 6:
                                TotKvarCnt += (MyDataHistory.Channel6 / ConversionFactor[5]);
                                KVAVal = (MyDataHistory.Channel6 / ConversionFactor[5]);
                                break;
                            case 7:
                                TotKvarCnt += (MyDataHistory.Channel7 / ConversionFactor[6]);
                                KVAVal = (MyDataHistory.Channel7 / ConversionFactor[6]);
                                break;
                            case 8:
                                TotKvarCnt += (MyDataHistory.Channel8 / ConversionFactor[7]);
                                KVAVal = (MyDataHistory.Channel8 / ConversionFactor[7]);
                                break;
                        }


                        periodCnt += 1;
                        if (kwhVal > MaxKwhDemand)
                        {
                            MaxKwhDemand = kwhVal;
                            MaxKwhDemandDate = MyDataHistory.TimeStamp;
                        }
                        if (KVAVal > MaxKvaDemand)
                        {
                            MaxKvaDemand = KVAVal;
                            MaxKvaDemandDate = MyDataHistory.TimeStamp;
                        }

                    }
                    catch (Exception ex)
                    {
                    }

                    // End If
                }
                try
                {
                    MyHtmlStr.Append("<table class=\"table table-striped responsive-utilities jambo_table bulk_action table-bordered\" BorderColor=\"#316AC5\" border=1 width=450 ><tr><td>");
                    MyHtmlStr.Append("Tarrif  :" + MyTarrif.TarrifDetails.TarriffName);
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Metering Interval  :" + MyPeriod.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");

                    MyHtmlStr.Append("Max Kwh</td><td>" + MaxKwhDemand.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kwh Date </td><td>" + MaxKwhDemandDate.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kva</td><td>" + MaxKvaDemand.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kva Date </td><td>" + MaxKvaDemandDate.ToString() + "</td></tr><tr><td>");

                    MyHtmlStr.Append("Total Kwh:" + TotKWhCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Total KW :" + TotKWCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Total KVA :" + TotKvarCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Billing :");
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    if (PeakKWhCost > 0)
                    {
                        if (PeakKWhCnt > 0)
                        {
                            MyHtmlStr.Append(PeakKWhLabel + "  " + PeakKWhCnt.ToString("#.00") + " KWh @ " + (PeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R" + ((PeakKWhCost * PeakKWhCnt) / 100).ToString("#.00"));
                            PeakKWhTotCost = (PeakKWhCost * PeakKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(PeakKWhLabel + "  " + PeakKWhCnt.ToString("#.00") + " KWh @ " + (PeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R0");
                            PeakKWhTotCost = 0;
                            //(PeakKWhCost * PeakKWhCnt) / 100
                        }
                        MyHtmlStr.Append("</td></tr><tr><td>");
                    }

                    if (StandKWhCost > 0)
                    {
                        if (StandKWhCnt > 0)
                        {
                            MyHtmlStr.Append(StandKWhLabel + "  " + StandKWhCnt.ToString("#.00") + " KWh @ " + (StandKWhCost / 100).ToString() + " R/KWh </td><td>   R" + ((StandKWhCost * StandKWhCnt) / 100).ToString("#.00"));
                            StandKWhTotCost = (StandKWhCost * StandKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(StandKWhLabel + "  " + StandKWhCnt.ToString("#.00") + " KWh @ " + (StandKWhCost / 100).ToString() + " R/KWh  </td><td>  R0");
                            StandKWhTotCost = 0;
                        }
                        MyHtmlStr.Append("</td></tr><tr><td>");
                    }
                    if (OffPeakKWhCost > 0)
                    {
                        if (OffPeakKWhCnt > 0)
                        {
                            MyHtmlStr.Append(OffPeakKWhLabel + "  " + OffPeakKWhCnt.ToString("#.00") + " KWh @ " + (OffPeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R" + ((OffPeakKWhCost * OffPeakKWhCnt) / 100).ToString("#.00"));
                            OffPeakKWhTotCost = (OffPeakKWhCost * OffPeakKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(OffPeakKWhLabel + "  " + OffPeakKWhCnt.ToString("#.00") + " KWh @ " + (OffPeakKWhCost / 100).ToString() + " R/KWh   </td><td> R0");
                            OffPeakKWhTotCost = 0;
                        }
                    }

                    double totCharge = 0;
                    try
                    {
                        double[] ChargeVals = new double[Information.UBound(MyTarrif.MeteringNetworkCharge)];
                        LiveMonitoring.IRemoteLib.MeteringNetworkCharges MyNetCharge = default(LiveMonitoring.IRemoteLib.MeteringNetworkCharges);
                        for (int myCnt = 0; myCnt <= Information.UBound(MyTarrif.MeteringNetworkCharge) - 1; myCnt++)
                        {
                            MyNetCharge = MyTarrif.MeteringNetworkCharge[myCnt];
                            if ((MyNetCharge == null) == false)
                            {
                                if (MyNetCharge.Percentage > 0)
                                {
                                    //calculate percentage
                                    double TotColVal = 0;
                                    string Cols = "";
                                    try
                                    {
                                        foreach (int MyColumns in MyNetCharge.Columns)
                                        {
                                            TotColVal += (ChargeVals[MyColumns] * (MyNetCharge.Percentage / 100));
                                            Cols += MyColumns.ToString() + ",";
                                        }
                                        Cols = Cols.Remove(Cols.LastIndexOf(","), 1);
                                        TotColVal += ((PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * (MyNetCharge.Percentage / 100));

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  (" + Cols + ")  @ " + (MyNetCharge.Percentage).ToString() + " %  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));

                                }
                                //If MyNetCharge.CostRperkVA > 0 Then
                                //    'calculate CostRperkVA
                                //    Dim TotColVal As Double = 0
                                //    Try
                                //        TotColVal = TotKvarCnt * MyNetCharge.CostRperkVA
                                //    Catch ex As Exception

                                //    End Try
                                //    ChargeVals(myCnt) = TotColVal
                                //    MyHtmlStr.Append("</td></tr><tr><td>")
                                //    MyHtmlStr.Append(MyNetCharge.ChargeName + "  " + TotKvarCnt.ToString("#.00") + "kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                //End If
                                if (MyNetCharge.CostRperkVA > 0)
                                {
                                    //calculate CostRperkVA
                                    //check for NMD value
                                    double TotColVal = 0;
                                    if (MyNetCharge.MaximumDemand > 0)
                                    {
                                        //ok bellow notified
                                        //SensorDet.ExtraValue1
                                        double CheckDemand = 0;
                                        //CheckDemand = SensorDet.ExtraValue1;
                                        DataManager myDataManager = new DataManager();
                                        CheckDemand = myDataManager.getMeterNMD(SensorDet.ID, DateTime.Now.Month, DateTime.Now.Year);
                                        if (MaxKvaDemand <= CheckDemand)
                                        {
                                            try
                                            {
                                                TotColVal = CheckDemand * MyNetCharge.CostRperkVA;

                                            }
                                            catch (Exception ex)
                                            {
                                            }
                                            ChargeVals[myCnt] = TotColVal;
                                            MyHtmlStr.Append("</td></tr><tr><td>");
                                            MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                        }
                                        else
                                        {
                                            //pay penalty above notified
                                            try
                                            {
                                                TotColVal = (CheckDemand * MyNetCharge.CostRperkVA) + (MaxKvaDemand - CheckDemand) * MyNetCharge.PenaltyCharge;

                                            }
                                            catch (Exception ex)
                                            {
                                            }
                                            ChargeVals[myCnt] = TotColVal;
                                            MyHtmlStr.Append("</td></tr><tr><td>");
                                            // MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  PEN: R" + MyNetCharge.PenaltyCharge.ToString + " PEN KVA:" + (TotKvarCnt - CheckDemand).ToString("#.00") + "  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                            // MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  NB:Above NMD  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                            MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA    </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                        }


                                    }
                                    else
                                    {
                                        try
                                        {
                                            TotColVal = MaxKvaDemand * MyNetCharge.CostRperkVA;

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        ChargeVals[myCnt] = TotColVal;
                                        MyHtmlStr.Append("</td></tr><tr><td>");
                                        MyHtmlStr.Append(MyNetCharge.ChargeName + "  " + MaxKvaDemand.ToString("#.00") + "kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                    }
                                }
                                if (MyNetCharge.FixedCost > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = MyNetCharge.FixedCost;
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.FixedCost).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperkWh > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    try
                                    {
                                        TotColVal = TotKWhCnt * MyNetCharge.CostRperkWh;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperkWh).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperMaxkVA > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    try
                                    {
                                        TotColVal = MaxKvaDemand * MyNetCharge.CostRperMaxkVA;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.CostRperMaxkVA
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperMaxkVA).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperday > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    long DayCnt = DateAndTime.DateDiff(DateInterval.Day, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));
                                    try
                                    {
                                        TotColVal = DayCnt * MyNetCharge.CostRperday;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.CostRperday
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperday).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                            }
                            totCharge += ChargeVals[myCnt];
                        }

                    }
                    catch (Exception ex)
                    {
                    }
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Subtotal  " + "  </td><td>  R" + (totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost).ToString("#.00"));
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Vat @ 15%  " + "  </td><td>  R" + ((totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * 0.15).ToString("#.00"));
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Total  " + "  </td><td>  R" + (totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost + ((totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * 0.14)).ToString("#.00"));

                    MyHtmlStr.Append("</td></tr></table>");

                }
                catch (Exception ex)
                {
                }

                if (GeneratePDF)
                {
                    Rep2HTML.Append(MyHtmlStr);
                }
                this.DivTarrifReport.InnerHtml = MyHtmlStr.ToString();
                AddPageBreak();

            }
            catch (Exception ex)
            {
            }


        }

        public void DrawElsterA1700TarrifReport(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringProfileRecord> MyData, List<LiveMonitoring.IRemoteLib.MeteringProfileMarker> MyDataMarkers, LiveMonitoring.IRemoteLib.MeteringTarrifDetails MyTarrif)
        {
            this.DivTarrifReport.Controls.Clear();
            try
            {
                StringBuilder MyHtmlStr = new StringBuilder();
                MyHtmlStr.Append("<table class=\"table table-striped responsive-utilities jambo_table bulk_action table-bordered\" BorderColor=\"#316AC5\" border=1><tr><td>");
                MyHtmlStr.Append("Tarrif Report:" + SensorDet.Caption);
                MyHtmlStr.Append("</td></tr><tr><td>");
                MyHtmlStr.Append("Meter :" + SensorDet.Caption);
                //ExcelStrData += "Sensor:" & SensorDet.Caption + vbCrLf
                MyHtmlStr.Append("</td></tr><tr><td>");
                MyHtmlStr.Append("From :" + Session["StartDate"] + " To :" + Session["EndDate"]);
                MyHtmlStr.Append("</td></tr></table>");

                ///''''''''''''''''''
                int Bcnt = 0;
                double MyPeriod = 30;
                //default to 30 minutes
                //check if we have a channel config else use default 69
                int myChannelConfig = 69;
                // LiveMonitoring.IRemoteLib.MeteringProfileMarker MyDataMarkerHistory = default(LiveMonitoring.IRemoteLib.MeteringProfileMarker);
                foreach (LiveMonitoring.IRemoteLib.MeteringProfileMarker MyDataMarkerHistory in MyDataMarkers)
                {
                    if (MyDataMarkerHistory.ProfileMarkerType == 1 | MyDataMarkerHistory.ProfileMarkerType == 4)
                    {
                        myChannelConfig = MyDataMarkerHistory.TRegisterConfigValue;
                        MyPeriod = returnMeteringPeriod(Convert.ToInt32(Conversion.Hex(MyDataMarkerHistory.Period).Substring(0, 1)));
                    }
                }
                //ok now we have channels set the names
                LiveMonitoring.IRemoteLib.ElsterA1700Data.TChannelConfig MyDataChannels = new LiveMonitoring.IRemoteLib.ElsterA1700Data.TChannelConfig(myChannelConfig);
                //Dim mychanel As LiveMonitoring.IRemoteLib.ElsterA1700Data.ChanelData

                //For Each mychanel In MyDataChannels.ChannelNames

                //Next

                double[] ConversionFactor = new double[10];
                int findMVA = 0;
                int findMW = 0;
                for (Bcnt = 0; Bcnt <= 7; Bcnt++)
                {
                    if (MyDataChannels.ChannelNames.Contains((Bcnt).ToString()) == true)
                    {
                        if (((LiveMonitoring.IRemoteLib.ElsterA1700Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelName == "KVA")
                        {
                            findMVA = Bcnt + 1;
                        }
                        if (((LiveMonitoring.IRemoteLib.ElsterA1700Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelName == "Import KW")
                        {
                            findMW = Bcnt + 1;
                        }
                        ConversionFactor[Bcnt] = ((LiveMonitoring.IRemoteLib.ElsterA1700Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelDivisorUnits;
                        //End If
                    }
                    else
                    {
                        ConversionFactor[Bcnt] = 0;
                    }
                }
                //pf possible
                if (MyDataChannels.Import_mW & MyDataChannels.mVA)
                {
                    ConversionFactor[8] = 0;
                }
                int tmp1cntwe = 0;
                //mycnt1= fields ?
                //LiveMonitoring.IRemoteLib.MeteringProfileRecord MyDataHistory = default(LiveMonitoring.IRemoteLib.MeteringProfileRecord);
                double TotKWhCnt = 0;
                double TotKWCnt = 0;
                double TotKvarCnt = 0;
                double PeakKWhCnt = 0;
                double StandKWhCnt = 0;
                double OffPeakKWhCnt = 0;
                double PeakKWhCost = 0;
                double StandKWhCost = 0;
                double OffPeakKWhCost = 0;
                double PeakKWhTotCost = 0;
                double StandKWhTotCost = 0;
                double OffPeakKWhTotCost = 0;
                string PeakKWhLabel = "";
                // = 0
                string StandKWhLabel = "";
                // = 0
                string OffPeakKWhLabel = "";
                // = 0

                double MaxKvaDemand = 0;
                System.DateTime MaxKvaDemandDate = default(System.DateTime);
                double MaxKwhDemand = 0;
                System.DateTime MaxKwhDemandDate = default(System.DateTime);
                double periodCnt = 0;
                double KVAVal = 0;
                double kwhVal = 0;

                int MaxFieldCnt = 0;
                bool firstrec = true;

                foreach (LiveMonitoring.IRemoteLib.MeteringProfileRecord MyDataHistory in MyData)
                {

                    try
                    {
                        // MyDataHistory.TimeStamp
                        //MyTarrif
                       // MyTarrif.ActiveEnergyCharges()
                        LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges TypePeriod = new LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges();
                        int TmpChargePeriod = 0;
                        //= FindPeriod(MyDataHistory.TimeStamp, MyTarrif.ActiveEnergyCharges, TypePeriod)
                        try
                        {
                            //Dim TypePeriod As New LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges
                            TmpChargePeriod = FindPeriod(MyDataHistory.TimeStamp, MyTarrif.ActiveEnergyCharges, ref TypePeriod);


                        }
                        catch (Exception ex)
                        {
                        }
                        if ((TypePeriod.ChargeName == null) == false & (TmpChargePeriod == 0) == false)
                        {
                            switch (TypePeriod.ChargePeriods[TmpChargePeriod].MeteringChargeType.ID)
                            {
                                //cant mix peak and offp eak so first one should be correct
                                case 1:
                                    //peak
                                    PeakKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                                    PeakKWhCost = TypePeriod.CostcPerKWh;
                                    PeakKWhLabel = TypePeriod.ChargeName;
                                    break;
                                case 2:
                                    //standard
                                    StandKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                                    StandKWhCost = TypePeriod.CostcPerKWh;
                                    StandKWhLabel = TypePeriod.ChargeName;
                                    break;
                                case 3:
                                    //off peak
                                    OffPeakKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                                    OffPeakKWhCost = TypePeriod.CostcPerKWh;
                                    OffPeakKWhLabel = TypePeriod.ChargeName;
                                    break;
                            }
                            //standard kwh by default
                        }
                        else
                        {
                            StandKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                            StandKWhCost = TypePeriod.CostcPerKWh;
                            StandKWhLabel = TypePeriod.ChargeName;
                        }


                        //KWH findMW
                        switch (findMW)
                        {
                            case 1:
                                //always 1 import w
                                TotKWhCnt += (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                                TotKWCnt += (MyDataHistory.Channel1 / ConversionFactor[0]);
                                break;
                        }
                        kwhVal = (MyDataHistory.Channel1 / ConversionFactor[0]) * (MyPeriod / 60);
                        //KVA findMVA
                        switch (findMVA)
                        {
                            case 1:
                                TotKvarCnt += (MyDataHistory.Channel1 / ConversionFactor[0]);
                                KVAVal = (MyDataHistory.Channel1 / ConversionFactor[0]);
                                break;
                            case 2:
                                TotKvarCnt += (MyDataHistory.Channel2 / ConversionFactor[1]);
                                KVAVal = (MyDataHistory.Channel2 / ConversionFactor[1]);
                                break;
                            //KVAVal = (MyDataHistory.Channel1 / ConversionFactor(1))
                            case 3:
                                TotKvarCnt += (MyDataHistory.Channel3 / ConversionFactor[2]);
                                KVAVal = (MyDataHistory.Channel3 / ConversionFactor[2]);
                                break;
                            case 4:
                                TotKvarCnt += (MyDataHistory.Channel4 / ConversionFactor[3]);
                                KVAVal = (MyDataHistory.Channel4 / ConversionFactor[3]);
                                break;
                            case 5:
                                TotKvarCnt += (MyDataHistory.Channel5 / ConversionFactor[4]);
                                KVAVal = (MyDataHistory.Channel5 / ConversionFactor[4]);
                                break;
                            case 6:
                                TotKvarCnt += (MyDataHistory.Channel6 / ConversionFactor[5]);
                                KVAVal = (MyDataHistory.Channel6 / ConversionFactor[5]);
                                break;
                            case 7:
                                TotKvarCnt += (MyDataHistory.Channel7 / ConversionFactor[6]);
                                KVAVal = (MyDataHistory.Channel7 / ConversionFactor[6]);
                                break;
                            case 8:
                                TotKvarCnt += (MyDataHistory.Channel8 / ConversionFactor[7]);
                                KVAVal = (MyDataHistory.Channel8 / ConversionFactor[7]);
                                break;
                        }


                        periodCnt += 1;
                        if (kwhVal > MaxKwhDemand)
                        {
                            MaxKwhDemand = kwhVal;
                            MaxKwhDemandDate = MyDataHistory.TimeStamp;
                        }
                        if (KVAVal > MaxKvaDemand)
                        {
                            MaxKvaDemand = KVAVal;
                            MaxKvaDemandDate = MyDataHistory.TimeStamp;
                        }

                    }
                    catch (Exception ex)
                    {
                    }

                    // End If
                }
                try
                {
                    MyHtmlStr.Append("<table class=\"table table-striped responsive-utilities jambo_table bulk_action table-bordered\" BorderColor=\"#316AC5\" border=1 width=450 ><tr><td>");
                    MyHtmlStr.Append("Tarrif  :" + MyTarrif.TarrifDetails.TarriffName);
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Metering Interval  :" + MyPeriod.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");

                    MyHtmlStr.Append("Max Kwh</td><td>" + MaxKwhDemand.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kwh Date </td><td>" + MaxKwhDemandDate.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kva</td><td>" + MaxKvaDemand.ToString() + "</td></tr><tr><td>");
                    MyHtmlStr.Append("Max Kva Date </td><td>" + MaxKvaDemandDate.ToString() + "</td></tr><tr><td>");

                    MyHtmlStr.Append("Total Kwh:" + TotKWhCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Total KW :" + TotKWCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Total KVA :" + TotKvarCnt.ToString());
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    MyHtmlStr.Append("Billing :");
                    MyHtmlStr.Append("</td><td></td></tr><tr><td>");
                    if (PeakKWhCost > 0)
                    {
                        if (PeakKWhCnt > 0)
                        {
                            MyHtmlStr.Append(PeakKWhLabel + "  " + PeakKWhCnt.ToString("#.00") + " KWh @ " + (PeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R" + ((PeakKWhCost * PeakKWhCnt) / 100).ToString("#.00"));
                            PeakKWhTotCost = (PeakKWhCost * PeakKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(PeakKWhLabel + "  " + PeakKWhCnt.ToString("#.00") + " KWh @ " + (PeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R0");
                            PeakKWhTotCost = 0;
                            //(PeakKWhCost * PeakKWhCnt) / 100
                        }
                        MyHtmlStr.Append("</td></tr><tr><td>");
                    }

                    if (StandKWhCost > 0)
                    {
                        if (StandKWhCnt > 0)
                        {
                            MyHtmlStr.Append(StandKWhLabel + "  " + StandKWhCnt.ToString("#.00") + " KWh @ " + (StandKWhCost / 100).ToString() + " R/KWh </td><td>   R" + ((StandKWhCost * StandKWhCnt) / 100).ToString("#.00"));
                            StandKWhTotCost = (StandKWhCost * StandKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(StandKWhLabel + "  " + StandKWhCnt.ToString("#.00") + " KWh @ " + (StandKWhCost / 100).ToString() + " R/KWh  </td><td>  R0");
                            StandKWhTotCost = 0;
                        }
                        MyHtmlStr.Append("</td></tr><tr><td>");
                    }
                    if (OffPeakKWhCost > 0)
                    {
                        if (OffPeakKWhCnt > 0)
                        {
                            MyHtmlStr.Append(OffPeakKWhLabel + "  " + OffPeakKWhCnt.ToString("#.00") + " KWh @ " + (OffPeakKWhCost / 100).ToString() + " R/KWh  </td><td>  R" + ((OffPeakKWhCost * OffPeakKWhCnt) / 100).ToString("#.00"));
                            OffPeakKWhTotCost = (OffPeakKWhCost * OffPeakKWhCnt) / 100;
                        }
                        else
                        {
                            MyHtmlStr.Append(OffPeakKWhLabel + "  " + OffPeakKWhCnt.ToString("#.00") + " KWh @ " + (OffPeakKWhCost / 100).ToString() + " R/KWh   </td><td> R0");
                            OffPeakKWhTotCost = 0;
                        }
                    }

                    double totCharge = 0;
                    try
                    {
                        double[] ChargeVals = new double[Information.UBound(MyTarrif.MeteringNetworkCharge)];
                        LiveMonitoring.IRemoteLib.MeteringNetworkCharges MyNetCharge = default(LiveMonitoring.IRemoteLib.MeteringNetworkCharges);
                        for (int myCnt = 0; myCnt <= Information.UBound(MyTarrif.MeteringNetworkCharge) - 1; myCnt++)
                        {
                            MyNetCharge = MyTarrif.MeteringNetworkCharge[myCnt];
                            if ((MyNetCharge == null) == false)
                            {
                                if (MyNetCharge.Percentage > 0)
                                {
                                    //calculate percentage
                                    double TotColVal = 0;
                                    string Cols = "";
                                    try
                                    {
                                        foreach (int MyColumns in MyNetCharge.Columns)
                                        {
                                            TotColVal += (ChargeVals[MyColumns] * (MyNetCharge.Percentage / 100));
                                            Cols += MyColumns.ToString() + ",";
                                        }
                                        Cols = Cols.Remove(Cols.LastIndexOf(","), 1);
                                        TotColVal += ((PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * (MyNetCharge.Percentage / 100));

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  (" + Cols + ")  @ " + (MyNetCharge.Percentage).ToString() + " %  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));

                                }
                                //If MyNetCharge.CostRperkVA > 0 Then
                                //    'calculate CostRperkVA
                                //    Dim TotColVal As Double = 0
                                //    Try
                                //        TotColVal = TotKvarCnt * MyNetCharge.CostRperkVA
                                //    Catch ex As Exception

                                //    End Try
                                //    ChargeVals(myCnt) = TotColVal
                                //    MyHtmlStr.Append("</td></tr><tr><td>")
                                //    MyHtmlStr.Append(MyNetCharge.ChargeName + "  " + TotKvarCnt.ToString("#.00") + "kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                //End If
                                if (MyNetCharge.CostRperkVA > 0)
                                {
                                    //calculate CostRperkVA
                                    //check for NMD value
                                    double TotColVal = 0;
                                    if (MyNetCharge.MaximumDemand > 0)
                                    {
                                        //ok bellow notified
                                        //SensorDet.ExtraValue1
                                        double CheckDemand = 0;
                                        // CheckDemand = SensorDet.ExtraValue1;
                                        DataManager myDataManager = new DataManager();
                                        CheckDemand = myDataManager.getMeterNMD(SensorDet.ID, DateTime.Now.Month, DateTime.Now.Year);
                                        if (MaxKvaDemand <= CheckDemand)
                                        {
                                            try
                                            {
                                                TotColVal = CheckDemand * MyNetCharge.CostRperkVA;

                                            }
                                            catch (Exception ex)
                                            {
                                            }
                                            ChargeVals[myCnt] = TotColVal;
                                            MyHtmlStr.Append("</td></tr><tr><td>");
                                            MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                        }
                                        else
                                        {
                                            //pay penalty above notified
                                            try
                                            {
                                                TotColVal = (CheckDemand * MyNetCharge.CostRperkVA) + (MaxKvaDemand - CheckDemand) * MyNetCharge.PenaltyCharge;

                                            }
                                            catch (Exception ex)
                                            {
                                            }
                                            ChargeVals[myCnt] = TotColVal;
                                            MyHtmlStr.Append("</td></tr><tr><td>");
                                            // MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  PEN: R" + MyNetCharge.PenaltyCharge.ToString + " PEN KVA:" + (TotKvarCnt - CheckDemand).ToString("#.00") + "  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                            // MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString + " R/kVA  NB:Above NMD  </td><td>  R" + (ChargeVals(myCnt)).ToString("#.00"))
                                            MyHtmlStr.Append(MyNetCharge.ChargeName + "  ND:" + CheckDemand.ToString("#.00") + " kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA    </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                        }


                                    }
                                    else
                                    {
                                        try
                                        {
                                            TotColVal = MaxKvaDemand * MyNetCharge.CostRperkVA;

                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        ChargeVals[myCnt] = TotColVal;
                                        MyHtmlStr.Append("</td></tr><tr><td>");
                                        MyHtmlStr.Append(MyNetCharge.ChargeName + "  " + MaxKvaDemand.ToString("#.00") + "kVA  @ " + (MyNetCharge.CostRperkVA).ToString() + " R/kVA  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                    }
                                }
                                if (MyNetCharge.FixedCost > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = MyNetCharge.FixedCost;
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.FixedCost).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperkWh > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    try
                                    {
                                        TotColVal = TotKWhCnt * MyNetCharge.CostRperkWh;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperkWh).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperMaxkVA > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    try
                                    {
                                        TotColVal = MaxKvaDemand * MyNetCharge.CostRperMaxkVA;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.CostRperMaxkVA
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperMaxkVA).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                                if (MyNetCharge.CostRperday > 0)
                                {
                                    //calculate FixedCost
                                    double TotColVal = 0;
                                    long DayCnt = DateAndTime.DateDiff(DateInterval.Day, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));
                                    try
                                    {
                                        TotColVal = DayCnt * MyNetCharge.CostRperday;

                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    //Dim TotColVal As Double = MyNetCharge.CostRperday
                                    ChargeVals[myCnt] = TotColVal;
                                    MyHtmlStr.Append("</td></tr><tr><td>");
                                    MyHtmlStr.Append(MyNetCharge.ChargeName + "  @ R" + (MyNetCharge.CostRperday).ToString() + "  </td><td>  R" + (ChargeVals[myCnt]).ToString("#.00"));
                                }
                            }
                            totCharge += ChargeVals[myCnt];
                        }

                    }
                    catch (Exception ex)
                    {
                    }
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Subtotal  " + "  </td><td>  R" + (totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost).ToString("#.00"));
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Vat @ 15%  " + "  </td><td>  R" + ((totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * 0.15).ToString("#.00"));
                    MyHtmlStr.Append("</td></tr><tr><td>");
                    MyHtmlStr.Append("Total  " + "  </td><td>  R" + (totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost + ((totCharge + PeakKWhTotCost + StandKWhTotCost + OffPeakKWhTotCost) * 0.14)).ToString("#.00"));

                    MyHtmlStr.Append("</td></tr></table>");

                }
                catch (Exception ex)
                {
                }


                this.DivTarrifReport.InnerHtml = MyHtmlStr.ToString();
                if (GeneratePDF)
                {
                    Rep2HTML.Append(MyHtmlStr);
                }
                AddPageBreak();

            }
            catch (Exception ex)
            {
            }


        }
        public int returnMeteringPeriod(int PEriodCode)
        {
            int retval = 30;
            try
            {
                switch (PEriodCode)
                {
                    case 0:
                        retval = 1;
                        break;
                    case 1:
                        retval = 2;
                        break;
                    case 2:
                        retval = 3;
                        break;
                    case 3:
                        retval = 4;
                        break;
                    case 4:
                        retval = 5;
                        break;
                    case 5:
                        retval = 6;
                        break;
                    case 6:
                        retval = 10;
                        break;
                    case 7:
                        retval = 15;
                        break;
                    case 8:
                        retval = 20;
                        break;
                    case 9:
                        retval = 30;
                        break;
                    case 10:
                        retval = 60;

                        break;
                }

            }
            catch (Exception ex)
            {
            }
            return retval;

        }
        public int FindPeriod(DateTime myCheckTime, LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges[] ActivePeriods, ref LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges MyActiveCahrge)
        {
            //Dim myretval As LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges 'off peak by default
            Debug.Write(myCheckTime.ToString());
            try
            {
                foreach (LiveMonitoring.IRemoteLib.MeteringActiveEnergyCharges myActiveEnergyPeriod in ActivePeriods)
                {
                    if ((myActiveEnergyPeriod == null) == false)
                    {
                        if (myActiveEnergyPeriod.FindPeriod(myCheckTime.Month))
                        {
                            //myActiveEnergyPeriod.ChargePeriods()
                            for (int MyPeriodcnt = 0; MyPeriodcnt <= Information.UBound(myActiveEnergyPeriod.ChargePeriods) - 1; MyPeriodcnt++)
                            {
                                LiveMonitoring.IRemoteLib.MeteringChargePeriods MyCharges = myActiveEnergyPeriod.ChargePeriods[MyPeriodcnt];
                                if (CheckPeriodDay(myCheckTime, MyCharges))
                                {
                                    //day match now time match
                                    if (CheckPeriodTime(myCheckTime, MyCharges))
                                    {
                                        //day match now time match
                                        //MyCharges.MeteringChargeType()
                                        MyActiveCahrge = myActiveEnergyPeriod;
                                        return MyPeriodcnt;
                                    }
                                }
                            }
                        }
                    }

                }


            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
            return 0;
        }
        public bool CheckPeriodTime(DateTime CheckTime, LiveMonitoring.IRemoteLib.MeteringChargePeriods MyCharges)
        {

            try
            {
                //Weekday so must match
                if (MyCharges.StartTime.TimeOfDay <= CheckTime.TimeOfDay & MyCharges.EndTime.TimeOfDay > CheckTime.TimeOfDay)
                {
                    return true;
                }

            }
            catch (Exception ex)
            {
            }
            return false;
        }
        public bool CheckPeriodDay(DateTime CheckTime, LiveMonitoring.IRemoteLib.MeteringChargePeriods MyCharges)
        {
            try
            {
                //everyday so must match
                if (MyCharges.Days == 7)
                {
                    return true;
                }
                //Weekday so must match
                if (MyCharges.Days == 8 & (CheckTime.DayOfWeek > 0 & ((int)CheckTime.DayOfWeek<  6)))
                {
                    return true;
                }
                //actual day must match
                if (MyCharges.Days < 7 & ((int)CheckTime.DayOfWeek == MyCharges.Days))
                {
                    return true;
                }


            }
            catch (Exception ex)
            {
            }
            return false;
        }
        public void AddLayer(LiveMonitoring.IRemoteLib.SensorDetails SensorDet)
        {
            //MyCollection
            List<LiveMonitoring.IRemoteLib.MeteringProfileRecord> MyData = null;
            List<LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord> MyDataHist = null;
            List<LiveMonitoring.IRemoteLib.MeteringProfileMarker> MyDataMarkers = null;
            LiveMonitoring.GlobalRemoteVars MyRem = new LiveMonitoring.GlobalRemoteVars();
            try
            {
                //if (Information.IsDate(Session["StartDate"]) == true & Information.IsDate(Session["EndDate"]) == true)
                //{
                    //If cmbDataSet.SelectedValue = 0 Then
                    if (SensorDet.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1140MeterReadProfile | SensorDet.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1700MeterReadProfile)
                    {
                        try
                        {
                            MyData = MyRem.LiveMonServer.GetMeteringProfileRecord(SensorDet.ID, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));

                        }
                        catch (Exception ex)
                        {
                        }
                        try
                        {
                            MyDataMarkers = MyRem.LiveMonServer.GetMeteringProfileMarkers(SensorDet.ID, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));

                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    if (SensorDet.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.RockwellPM1000EnergyLogResults)
                    {
                        try
                        {
                            MyDataHist = MyRem.LiveMonServer.GetMeteringDataHistoryRecord(SensorDet.ID, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));

                        }
                        catch (Exception ex)
                        {
                        }
                    }
                if (SensorDet.Type == LiveMonitoring.IRemoteLib.SensorDetails.SensorType.LandisGyrE650Profile)
                {
                    try
                    {
                        MyDataHist = MyRem.LiveMonServer.GetMeteringDataHistoryRecord(SensorDet.ID, Convert.ToDateTime(Session["StartDate"]), Convert.ToDateTime(Session["EndDate"]));

                    }
                    catch (Exception ex)
                    {
                    }
                }

                //Else
                //MyData = MyRem.server1.GetFilteredSensorHistory(cmbDataSet.SelectedValue, SensorDet.ID, CDate(Session("StartDate")), CDate(Session("EndDate")))
                // End If
                //Try
                //    'GetMeteringTarrifEvent
                //    MyTarrif = MyRem.LiveMonServer.GetMeteringTarrif(TarrifList.SelectedValue)
                //Catch ex As Exception

                //End Try

                //Else
                //MyData = MyRem.server1.GetFilteredSensorHistory(cmbDataSet.SelectedValue, SensorDet.ID, CDate(Session("StartDate")), CDate(Session("EndDate")))
                // End If


            }
            catch (Exception ex)
            {
            }

            switch (SensorDet.Type)
            {
                case LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1140MeterReadProfile:
                    if ((MyData == null) == false)
                    {
                        DrawElsterA1140ProfileGraphs(SensorDet, MyData, MyDataMarkers);
                    }

                    break;
                case LiveMonitoring.IRemoteLib.SensorDetails.SensorType.ElsterA1700MeterReadProfile:
                    if ((MyData == null) == false)
                    {
                        DrawElsterA1700ProfileGraphs(SensorDet, MyData, MyDataMarkers);
                    }

                    break;
                case LiveMonitoring.IRemoteLib.SensorDetails.SensorType.RockwellPM1000EnergyLogResults:
                    if ((MyDataHist == null) == false)
                    {
                        DrawRockwellPM1000EnergyProfileGraphs(SensorDet, MyDataHist);
                    }

                    break;
                case LiveMonitoring.IRemoteLib.SensorDetails.SensorType.LandisGyrE650Profile:
                    if ((MyDataHist == null) == false)
                    {
                        DrawLandisProfileGraphs(SensorDet, MyDataHist);
                    }

                    break;
            }



        }

        public void DrawHtmlLine()
        {
            StringBuilder MyHtmlStr = new StringBuilder();
            MyHtmlStr.Append("<br/><hr/><br/>");
        }
        public void DrawLandisProfileGraphs(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord> MyDataHist)
        {
            try
            {
                DrawLandisProfileGraphs2(SensorDet, MyDataHist);
                //Active Power
                AddPageBreak();

            }
            catch (Exception ex)
            {
            }
        }
        public void DrawLandisProfileGraphs2(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord> MyDataHist)
        {
            try
            {
                //attach event for legent width
                // MyDateLinechart1.EnableCrossHair = this.CheckBox1.Checked;

                MyDateLinechart1.ChartDrawItem += ultraChart1_ChartDrawItem;
                MyDateLinechart1.ID = "LandisPM1000EP" + SensorDet.ID.ToString();
                MyDateLinechart1.TitleTop.Extent = 45;
                //sets the font color
                MyDateLinechart1.TitleTop.FontColor = Color.Red;
                //sets whether the chart auto-sizes the font for the title
                //MyDateLinechart1.TitleTop.FontSizeBestFit = false
                //sets the horizontal alignment of the text
                MyDateLinechart1.TitleTop.HorizontalAlign = StringAlignment.Center;
                //sets the margins for the Top, Bottom, Left and right
                MyDateLinechart1.TitleTop.Margins.Bottom = 2;
                MyDateLinechart1.TitleTop.Margins.Top = 2;
                MyDateLinechart1.TitleTop.Margins.Left = 2;
                MyDateLinechart1.TitleTop.Margins.Right = 2;
                //sets the text to display for the chart in the title
                MyDateLinechart1.TitleTop.Text = "Landis Profile Graphs :" + SensorDet.Caption;
                //sets the vertical alignment of the title
                MyDateLinechart1.TitleTop.VerticalAlign = StringAlignment.Near;
                //show/hide the referenced title
                MyDateLinechart1.TitleTop.Visible = true;
                //wrap/don't wrap the text
                MyDateLinechart1.TitleTop.WrapText = true;
                // Set composite charts
                MyDateLinechart1.ChartType = ChartType.Composite;
                // Create the ChartArea
                ChartArea myDLChartArea1 = new ChartArea();
                //Dim myDLChartArea2 As New ChartArea()
                // Add the Chart Area to the ChartAreas collection
                MyDateLinechart1.CompositeChart.ChartAreas.Add(myDLChartArea1);
                //MyDateLinechart1.CompositeChart.ChartAreas.Add(myDLChartArea2)
                // Create the ChartLayer
                ChartLayerAppearance DLchartLayer1 = new ChartLayerAppearance();
                ChartLayerAppearance DLchartLayer2 = new ChartLayerAppearance();
                // Set the ChartType
                DLchartLayer1.ChartType = ChartType.LineChart;
                //DLchartLayer1.Key = "1"
                //DLchartLayer1.ChartLayer.LayerID = "1"
                DLchartLayer2.ChartType = ChartType.LineChart;
                //DLchartLayer2.Key = "2"
                //DLchartLayer2.ChartLayer.LayerID = "2"
                // Create an X axis
                AxisItem DLxAxis1 = new AxisItem();
                DLxAxis1.axisNumber = AxisNumber.X_Axis;
                DLxAxis1.DataType = AxisDataType.String;
                DLxAxis1.Labels.ItemFormatString = "<ITEM_LABEL>";
                DLxAxis1.Labels.Font = new Font("Tahoma", 7);
                DLxAxis1.LineThickness = 1;
                //Dim DLxAxis2 As New AxisItem()
                //DLxAxis2.axisNumber = AxisNumber.X_Axis
                //DLxAxis2.DataType = AxisDataType.String
                //DLxAxis2.Labels.ItemFormatString = "<ITEM_LABEL>"
                //DLxAxis2.Labels.Font = New Font("Tahoma", 7)
                //DLxAxis2.LineThickness = 1
                // Create an Y axis
                AxisItem DLyAxis1 = new AxisItem();
                DLyAxis1.axisNumber = AxisNumber.Y_Axis;
                DLyAxis1.Labels.ItemFormatString = "<DATA_VALUE:0.00#>";
                DLyAxis1.Labels.Font = new Font("Tahoma", 7);
                DLyAxis1.LineThickness = 1;
                AxisItem DLyAxis2 = new AxisItem();
                DLyAxis2.axisNumber = AxisNumber.Y2_Axis;
                DLyAxis2.Labels.ItemFormatString = "<DATA_VALUE:0.00#>";
                DLyAxis2.Labels.Font = new Font("Tahoma", 7);
                DLyAxis2.LineThickness = 1;
                myDLChartArea1.Axes.Add(DLxAxis1);
                myDLChartArea1.Axes.Add(DLyAxis1);
                //myDLChartArea1.Axes.Add(DLxAxis1)
                myDLChartArea1.Axes.Add(DLyAxis2);
                // Set the axes
                DLchartLayer1.AxisX = DLxAxis1;
                DLchartLayer1.AxisY = DLyAxis1;
                DLchartLayer2.AxisX = DLxAxis1;
                DLchartLayer2.AxisY = DLyAxis2;
                // Set the ChartArea
                DLchartLayer1.ChartArea = myDLChartArea1;
                DLchartLayer2.ChartArea = myDLChartArea1;
                // Add the ChartLayer to the ChartLayers collection
                MyDateLinechart1.CompositeChart.ChartLayers.Add(DLchartLayer1);
                MyDateLinechart1.CompositeChart.ChartLayers.Add(DLchartLayer2);
                ///''''''''''''''''''
                ISeries DLseries1 = null;
                NumericTimeSeries[] numericTimeSeries1 = new NumericTimeSeries[5];
                int Bcnt = 0;
                double[] ConversionFactor = new double[10];
                int findMVA = 0;

                numericTimeSeries1[0] = new NumericTimeSeries();
                numericTimeSeries1[0].Label = "kWh";
                ConversionFactor[0] = 0;

                numericTimeSeries1[1] = new NumericTimeSeries();
                numericTimeSeries1[1].Label = "kVARh";
                ConversionFactor[1] = 0;

                numericTimeSeries1[2] = new NumericTimeSeries();
                numericTimeSeries1[2].Label = "kVAh";
                ConversionFactor[2] = 0;

                numericTimeSeries1[3] = new NumericTimeSeries();
                numericTimeSeries1[3].Label = "Power Factor";
                ConversionFactor[3] = 0;


                int tmp1cntwe = 0;
                //mycnt1= fields ?
                //LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyDataHistory = default(LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord);
                LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyPrevDataHistory = default(LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord);
                int MaxFieldCnt = 0;
                bool firstrec = true;

                foreach (LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyDataHistory in MyDataHist)
                {
                    int myret = 0;
                    // Dim ConversionFactor As Double = 0
                    try
                    {
                        if (firstrec)
                        {
                            firstrec = false;
                            txtStart.Text = MyDataHistory.TimeStamp.ToLongDateString();
                            MyPrevDataHistory = MyDataHistory;
                        }
                        else
                        {
                            try
                            {
                                txtEnd.Text = MyDataHistory.TimeStamp.ToString();

                            }
                            catch (Exception ex)
                            {
                            }
                            try
                            {
                                myret = numericTimeSeries1[0].Points.Add(new NumericTimeDataPoint());
                                numericTimeSeries1[0].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                                numericTimeSeries1[0].Points[myret].NumericValue = MyDataHistory.kWhValue ;

                                myret = numericTimeSeries1[1].Points.Add(new NumericTimeDataPoint());
                                numericTimeSeries1[1].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                                numericTimeSeries1[1].Points[myret].NumericValue = MyDataHistory.kVARValue ;

                                myret = numericTimeSeries1[2].Points.Add(new NumericTimeDataPoint());
                                numericTimeSeries1[2].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                                numericTimeSeries1[2].Points[myret].NumericValue = MyDataHistory.kVAValue ;

                                myret = numericTimeSeries1[3].Points.Add(new NumericTimeDataPoint());
                                numericTimeSeries1[3].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                                numericTimeSeries1[3].Points[myret].NumericValue = MyDataHistory.PfValue;

                                MyPrevDataHistory = MyDataHistory;


                            }
                            catch (Exception ex)
                            {
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                    }


                    // End If
                }

                int Acnt = 0;
                for (Acnt = 0; Acnt <= 2; Acnt++)
                {
                    DLchartLayer1.Series.Add(numericTimeSeries1[Acnt]);
                    DLseries1 = numericTimeSeries1[Acnt];
                    MyDateLinechart1.Series.Add(DLseries1);
                }
                //pf
                DLchartLayer2.Series.Add(numericTimeSeries1[3]);
                DLseries1 = numericTimeSeries1[3];
                MyDateLinechart1.Series.Add(DLseries1);

                ///''''''''''''''''''
                // Set X axis
                DLxAxis1.Labels.Orientation = TextOrientation.VerticalLeftFacing;
                DLxAxis1.Labels.ItemFormatString = "<ITEM_LABEL:MM/dd/yyyy HH:mm>";
                //DLxAxis2.Labels.Orientation = TextOrientation.VerticalLeftFacing
                //DLxAxis2.Labels.ItemFormatString = "<ITEM_LABEL:MM/dd/yyyy HH:mm>"
                // Set Y axis
                // Set the ChartType
                DLchartLayer1.ChartType = ChartType.LineChart;
                DLchartLayer2.ChartType = ChartType.LineChart;
                // Set Axis Type
                SetAxisTypes(DLchartLayer1);
                SetAxisTypes(DLchartLayer2);
                //DLchartLayer1.AxisY.Extent = 130
                //Dim atype As New AxisDataType
                DLchartLayer1.AxisX.DataType = AxisDataType.Time;
                DLchartLayer1.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
                DLchartLayer1.AxisY.DataType = AxisDataType.Numeric;

                DLchartLayer2.AxisX.DataType = AxisDataType.Time;
                DLchartLayer2.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
                DLchartLayer2.AxisY.DataType = AxisDataType.Numeric;
                // Add the series to the ChartLayer's Series collection.
                MyDateLinechart1.Width = 1200;
                MyDateLinechart1.Height = 400;
                CompositeLegend legend1 = new CompositeLegend();
                legend1.LabelStyle.Font = new Font("Times New Roman", 10);
                MyDateLinechart1.CompositeChart.Legends.Add(legend1);
                //MyDateLinechart1.CompositeChart.ChartLayers(0).ChartLayer.LayerID = "1"
                // MyDateLinechart1.CompositeChart.ChartLayers(1).ChartLayer.LayerID = "2"
                legend1.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers[0]);
                legend1.BoundsMeasureType = MeasureType.Percentage;
                legend1.Bounds = new Rectangle(1, 1, 10, 10);
                CompositeLegend legend2 = new CompositeLegend();
                legend2.LabelStyle.Font = new Font("Times New Roman", 10);
                MyDateLinechart1.CompositeChart.Legends.Add(legend2);
                legend2.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers[1]);
                legend2.BoundsMeasureType = MeasureType.Percentage;
                legend2.Bounds = new Rectangle(88, 1, 11, 7);
                //right,top,width,height
                MyDateLinechart1.CompositeChart.ChartAreas[0].BoundsMeasureType = MeasureType.Percentage;
                MyDateLinechart1.CompositeChart.ChartAreas[0].Bounds = new Rectangle(5, 0, 90, 100);
                Infragistics.UltraChart.Resources.Appearance.BorderAppearance MyBorder = new Infragistics.UltraChart.Resources.Appearance.BorderAppearance();
                MyBorder.Thickness = 0;
                MyDateLinechart1.CompositeChart.ChartAreas[0].Border = MyBorder;
                //MyDateLinechart1.CompositeChart.ChartAreas(1).BoundsMeasureType = MeasureType.Percentage
                //MyDateLinechart1.CompositeChart.ChartAreas(1).Bounds = New Rectangle(0, 20, 100, 80)
                this.Charts.Controls.Add(MyDateLinechart1);
                //Dim legend1 As New CompositeLegend()
                //legend1.LabelStyle.Font = New Font("Times New Roman", 10)
                //MyDateLinechart1.CompositeChart.Legends.Add(legend1)
                //legend1.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers(0))
                //legend1.BoundsMeasureType = MeasureType.Percentage
                // legend1.Bounds = New Rectangle(5, 5, 105, 14)
                if (GeneratePDF)
                {
                    MemoryStream ms = new MemoryStream();
                    //= New MemoryStream(myImageBytes, 0, myImageBytes.Length)
                    MyDateLinechart1.SaveTo(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    System.Drawing.Image lastimage = default(System.Drawing.Image);
                    lastimage = System.Drawing.Image.FromStream(ms);
                    lastimage.Tag = SensorDet.Caption;
                    Rep2Images.Add(lastimage, MyDateLinechart1.ID);
                }

            }
            catch (Exception ex)
            {
            }

        }
        public void DrawRockwellPM1000EnergyProfileGraphs(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord> MyDataHist)
        {
            try
            {
                DrawRockwellPM1000EnergyProfileGraphs2(SensorDet, MyDataHist);
                //Active Power
                AddPageBreak();

            }
            catch (Exception ex)
            {
            }
        }
        public void DrawRockwellPM1000EnergyProfileGraphs2(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord> MyDataHist)
        {
            try
            {
                //attach event for legent width
               // MyDateLinechart1.EnableCrossHair = this.CheckBox1.Checked;

                MyDateLinechart1.ChartDrawItem += ultraChart1_ChartDrawItem;
                MyDateLinechart1.ID = "RockwellPM1000EP" + SensorDet.ID.ToString();
                MyDateLinechart1.TitleTop.Extent = 45;
                //sets the font color
                MyDateLinechart1.TitleTop.FontColor = Color.Red;
                //sets whether the chart auto-sizes the font for the title
                //MyDateLinechart1.TitleTop.FontSizeBestFit = false
                //sets the horizontal alignment of the text
                MyDateLinechart1.TitleTop.HorizontalAlign = StringAlignment.Center;
                //sets the margins for the Top, Bottom, Left and right
                MyDateLinechart1.TitleTop.Margins.Bottom = 2;
                MyDateLinechart1.TitleTop.Margins.Top = 2;
                MyDateLinechart1.TitleTop.Margins.Left = 2;
                MyDateLinechart1.TitleTop.Margins.Right = 2;
                //sets the text to display for the chart in the title
                MyDateLinechart1.TitleTop.Text = "Rockwell Profile Graphs :" + SensorDet.Caption;
                //sets the vertical alignment of the title
                MyDateLinechart1.TitleTop.VerticalAlign = StringAlignment.Near;
                //show/hide the referenced title
                MyDateLinechart1.TitleTop.Visible = true;
                //wrap/don't wrap the text
                MyDateLinechart1.TitleTop.WrapText = true;
                // Set composite charts
                MyDateLinechart1.ChartType = ChartType.Composite;
                // Create the ChartArea
                ChartArea myDLChartArea1 = new ChartArea();
                //Dim myDLChartArea2 As New ChartArea()
                // Add the Chart Area to the ChartAreas collection
                MyDateLinechart1.CompositeChart.ChartAreas.Add(myDLChartArea1);
                //MyDateLinechart1.CompositeChart.ChartAreas.Add(myDLChartArea2)
                // Create the ChartLayer
                ChartLayerAppearance DLchartLayer1 = new ChartLayerAppearance();
                ChartLayerAppearance DLchartLayer2 = new ChartLayerAppearance();
                // Set the ChartType
                DLchartLayer1.ChartType = ChartType.LineChart;
                //DLchartLayer1.Key = "1"
                //DLchartLayer1.ChartLayer.LayerID = "1"
                DLchartLayer2.ChartType = ChartType.LineChart;
                //DLchartLayer2.Key = "2"
                //DLchartLayer2.ChartLayer.LayerID = "2"
                // Create an X axis
                AxisItem DLxAxis1 = new AxisItem();
                DLxAxis1.axisNumber = AxisNumber.X_Axis;
                DLxAxis1.DataType = AxisDataType.String;
                DLxAxis1.Labels.ItemFormatString = "<ITEM_LABEL>";
                DLxAxis1.Labels.Font = new Font("Tahoma", 7);
                DLxAxis1.LineThickness = 1;
                //Dim DLxAxis2 As New AxisItem()
                //DLxAxis2.axisNumber = AxisNumber.X_Axis
                //DLxAxis2.DataType = AxisDataType.String
                //DLxAxis2.Labels.ItemFormatString = "<ITEM_LABEL>"
                //DLxAxis2.Labels.Font = New Font("Tahoma", 7)
                //DLxAxis2.LineThickness = 1
                // Create an Y axis
                AxisItem DLyAxis1 = new AxisItem();
                DLyAxis1.axisNumber = AxisNumber.Y_Axis;
                DLyAxis1.Labels.ItemFormatString = "<DATA_VALUE:0.00#>";
                DLyAxis1.Labels.Font = new Font("Tahoma", 7);
                DLyAxis1.LineThickness = 1;
                AxisItem DLyAxis2 = new AxisItem();
                DLyAxis2.axisNumber = AxisNumber.Y2_Axis;
                DLyAxis2.Labels.ItemFormatString = "<DATA_VALUE:0.00#>";
                DLyAxis2.Labels.Font = new Font("Tahoma", 7);
                DLyAxis2.LineThickness = 1;
                myDLChartArea1.Axes.Add(DLxAxis1);
                myDLChartArea1.Axes.Add(DLyAxis1);
                //myDLChartArea1.Axes.Add(DLxAxis1)
                myDLChartArea1.Axes.Add(DLyAxis2);
                // Set the axes
                DLchartLayer1.AxisX = DLxAxis1;
                DLchartLayer1.AxisY = DLyAxis1;
                DLchartLayer2.AxisX = DLxAxis1;
                DLchartLayer2.AxisY = DLyAxis2;
                // Set the ChartArea
                DLchartLayer1.ChartArea = myDLChartArea1;
                DLchartLayer2.ChartArea = myDLChartArea1;
                // Add the ChartLayer to the ChartLayers collection
                MyDateLinechart1.CompositeChart.ChartLayers.Add(DLchartLayer1);
                MyDateLinechart1.CompositeChart.ChartLayers.Add(DLchartLayer2);
                ///''''''''''''''''''
                ISeries DLseries1 = null;
                NumericTimeSeries[] numericTimeSeries1 = new NumericTimeSeries[5];
                int Bcnt = 0;
                double[] ConversionFactor = new double[10];
                int findMVA = 0;

                numericTimeSeries1[0] = new NumericTimeSeries();
                numericTimeSeries1[0].Label = "kWh";
                ConversionFactor[0] = 0;

                numericTimeSeries1[1] = new NumericTimeSeries();
                numericTimeSeries1[1].Label = "kVARh";
                ConversionFactor[1] = 0;

                numericTimeSeries1[2] = new NumericTimeSeries();
                numericTimeSeries1[2].Label = "kVAh";
                ConversionFactor[2] = 0;

                numericTimeSeries1[3] = new NumericTimeSeries();
                numericTimeSeries1[3].Label = "Power Factor";
                ConversionFactor[3] = 0;


                int tmp1cntwe = 0;
                //mycnt1= fields ?
                //LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyDataHistory = default(LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord);
                LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyPrevDataHistory = default(LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord);
                int MaxFieldCnt = 0;
                bool firstrec = true;

                foreach (LiveMonitoring.IRemoteLib.MeteringDataHistoryRecord MyDataHistory in MyDataHist)
                {
                    int myret = 0;
                    // Dim ConversionFactor As Double = 0
                    try
                    {
                        if (firstrec)
                        {
                            firstrec = false;
                            txtStart.Text = MyDataHistory.TimeStamp.ToLongDateString();
                            MyPrevDataHistory = MyDataHistory;
                        }
                        else
                        {
                            try
                            {
                                txtEnd.Text = MyDataHistory.TimeStamp.ToString();

                            }
                            catch (Exception ex)
                            {
                            }
                            try
                            {
                                myret = numericTimeSeries1[0].Points.Add(new NumericTimeDataPoint());
                                numericTimeSeries1[0].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                                numericTimeSeries1[0].Points[myret].NumericValue = MyDataHistory.kWhValue - MyPrevDataHistory.kWhValue;

                                myret = numericTimeSeries1[1].Points.Add(new NumericTimeDataPoint());
                                numericTimeSeries1[1].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                                numericTimeSeries1[1].Points[myret].NumericValue = MyDataHistory.kVARValue - MyPrevDataHistory.kVARValue;

                                myret = numericTimeSeries1[2].Points.Add(new NumericTimeDataPoint());
                                numericTimeSeries1[2].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                                numericTimeSeries1[2].Points[myret].NumericValue = MyDataHistory.kVAValue - MyPrevDataHistory.kVAValue;

                                myret = numericTimeSeries1[3].Points.Add(new NumericTimeDataPoint());
                                numericTimeSeries1[3].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                                numericTimeSeries1[3].Points[myret].NumericValue = MyDataHistory.PfValue;

                                MyPrevDataHistory = MyDataHistory;


                            }
                            catch (Exception ex)
                            {
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                    }


                    // End If
                }

                int Acnt = 0;
                for (Acnt = 0; Acnt <= 2; Acnt++)
                {
                    DLchartLayer1.Series.Add(numericTimeSeries1[Acnt]);
                    DLseries1 = numericTimeSeries1[Acnt];
                    MyDateLinechart1.Series.Add(DLseries1);
                }
                //pf
                DLchartLayer2.Series.Add(numericTimeSeries1[3]);
                DLseries1 = numericTimeSeries1[3];
                MyDateLinechart1.Series.Add(DLseries1);

                ///''''''''''''''''''
                // Set X axis
                DLxAxis1.Labels.Orientation = TextOrientation.VerticalLeftFacing;
                DLxAxis1.Labels.ItemFormatString = "<ITEM_LABEL:MM/dd/yyyy HH:mm>";
                //DLxAxis2.Labels.Orientation = TextOrientation.VerticalLeftFacing
                //DLxAxis2.Labels.ItemFormatString = "<ITEM_LABEL:MM/dd/yyyy HH:mm>"
                // Set Y axis
                // Set the ChartType
                DLchartLayer1.ChartType = ChartType.LineChart;
                DLchartLayer2.ChartType = ChartType.LineChart;
                // Set Axis Type
                SetAxisTypes(DLchartLayer1);
                SetAxisTypes(DLchartLayer2);
                //DLchartLayer1.AxisY.Extent = 130
                //Dim atype As New AxisDataType
                DLchartLayer1.AxisX.DataType = AxisDataType.Time;
                DLchartLayer1.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
                DLchartLayer1.AxisY.DataType = AxisDataType.Numeric;

                DLchartLayer2.AxisX.DataType = AxisDataType.Time;
                DLchartLayer2.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
                DLchartLayer2.AxisY.DataType = AxisDataType.Numeric;
                // Add the series to the ChartLayer's Series collection.
                MyDateLinechart1.Width = 1200;
                MyDateLinechart1.Height = 400;
                CompositeLegend legend1 = new CompositeLegend();
                legend1.LabelStyle.Font = new Font("Times New Roman", 10);
                MyDateLinechart1.CompositeChart.Legends.Add(legend1);
                //MyDateLinechart1.CompositeChart.ChartLayers(0).ChartLayer.LayerID = "1"
                // MyDateLinechart1.CompositeChart.ChartLayers(1).ChartLayer.LayerID = "2"
                legend1.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers[0]);
                legend1.BoundsMeasureType = MeasureType.Percentage;
                legend1.Bounds = new Rectangle(1, 1, 10, 10);
                CompositeLegend legend2 = new CompositeLegend();
                legend2.LabelStyle.Font = new Font("Times New Roman", 10);
                MyDateLinechart1.CompositeChart.Legends.Add(legend2);
                legend2.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers[1]);
                legend2.BoundsMeasureType = MeasureType.Percentage;
                legend2.Bounds = new Rectangle(88, 1, 11, 7);
                //right,top,width,height
                MyDateLinechart1.CompositeChart.ChartAreas[0].BoundsMeasureType = MeasureType.Percentage;
                MyDateLinechart1.CompositeChart.ChartAreas[0].Bounds = new Rectangle(5, 0, 90, 100);
                Infragistics.UltraChart.Resources.Appearance.BorderAppearance MyBorder = new Infragistics.UltraChart.Resources.Appearance.BorderAppearance();
                MyBorder.Thickness = 0;
                MyDateLinechart1.CompositeChart.ChartAreas[0].Border = MyBorder;
                //MyDateLinechart1.CompositeChart.ChartAreas(1).BoundsMeasureType = MeasureType.Percentage
                //MyDateLinechart1.CompositeChart.ChartAreas(1).Bounds = New Rectangle(0, 20, 100, 80)
                this.Charts.Controls.Add(MyDateLinechart1);
                //Dim legend1 As New CompositeLegend()
                //legend1.LabelStyle.Font = New Font("Times New Roman", 10)
                //MyDateLinechart1.CompositeChart.Legends.Add(legend1)
                //legend1.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers(0))
                //legend1.BoundsMeasureType = MeasureType.Percentage
                // legend1.Bounds = New Rectangle(5, 5, 105, 14)
                if (GeneratePDF)
                {
                    MemoryStream ms = new MemoryStream();
                    //= New MemoryStream(myImageBytes, 0, myImageBytes.Length)
                    MyDateLinechart1.SaveTo(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    System.Drawing.Image lastimage = default(System.Drawing.Image);
                    lastimage = System.Drawing.Image.FromStream(ms);
                    lastimage.Tag = SensorDet.Caption;
                    Rep2Images.Add(lastimage,MyDateLinechart1.ID);
                }

            }
            catch (Exception ex)
            {
            }

        }

        public void DrawElsterA1700ProfileGraphs(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringProfileRecord> MyData, List<LiveMonitoring.IRemoteLib.MeteringProfileMarker> MyDataMarkers)
        {
            try
            {
                DrawElsterA1700ProfileGraphs2(SensorDet, MyData, MyDataMarkers);
                //Active Power
                AddPageBreak();

            }
            catch (Exception ex)
            {
            }


        }

        public void DrawElsterA1700ProfileGraphs2(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringProfileRecord> MyData, List<LiveMonitoring.IRemoteLib.MeteringProfileMarker> MyDataMarkers)
        {
            //attach event for legent width
            //MyDateLinechart1.EnableCrossHair = this.CheckBox1.Checked;

            MyDateLinechart1.ChartDrawItem += ultraChart1_ChartDrawItem;
            MyDateLinechart1.ID = "ElsterA1700PG" + SensorDet.ID.ToString();
            MyDateLinechart1.TitleTop.Extent = 45;
            //sets the font color
            MyDateLinechart1.TitleTop.FontColor = Color.Red;
            //sets whether the chart auto-sizes the font for the title
            //MyDateLinechart1.TitleTop.FontSizeBestFit = false
            //sets the horizontal alignment of the text
            MyDateLinechart1.TitleTop.HorizontalAlign = StringAlignment.Center;
            //sets the margins for the Top, Bottom, Left and right
            MyDateLinechart1.TitleTop.Margins.Bottom = 2;
            MyDateLinechart1.TitleTop.Margins.Top = 2;
            MyDateLinechart1.TitleTop.Margins.Left = 2;
            MyDateLinechart1.TitleTop.Margins.Right = 2;
            //sets the text to display for the chart in the title
            MyDateLinechart1.TitleTop.Text = "Elster Profile Graphs :" + SensorDet.Caption;
            //sets the vertical alignment of the title
            MyDateLinechart1.TitleTop.VerticalAlign = StringAlignment.Near;
            //show/hide the referenced title
            MyDateLinechart1.TitleTop.Visible = true;
            //wrap/don't wrap the text
            MyDateLinechart1.TitleTop.WrapText = true;
            // Set composite charts
            MyDateLinechart1.ChartType = ChartType.Composite;
            // Create the ChartArea
            ChartArea myDLChartArea1 = new ChartArea();
            //Dim myDLChartArea2 As New ChartArea()
            // Add the Chart Area to the ChartAreas collection
            MyDateLinechart1.CompositeChart.ChartAreas.Add(myDLChartArea1);
            //MyDateLinechart1.CompositeChart.ChartAreas.Add(myDLChartArea2)
            // Create the ChartLayer
            ChartLayerAppearance DLchartLayer1 = new ChartLayerAppearance();
            ChartLayerAppearance DLchartLayer2 = new ChartLayerAppearance();
            // Set the ChartType
            DLchartLayer1.ChartType = ChartType.LineChart;
            //DLchartLayer1.Key = "1"
            //DLchartLayer1.ChartLayer.LayerID = "1"
            DLchartLayer2.ChartType = ChartType.LineChart;
            //DLchartLayer2.Key = "2"
            //DLchartLayer2.ChartLayer.LayerID = "2"
            // Create an X axis
            AxisItem DLxAxis1 = new AxisItem();
            DLxAxis1.axisNumber = AxisNumber.X_Axis;
            DLxAxis1.DataType = AxisDataType.String;
            DLxAxis1.Labels.ItemFormatString = "<ITEM_LABEL>";
            DLxAxis1.Labels.Font = new Font("Tahoma", 7);
            DLxAxis1.LineThickness = 1;
            //Dim DLxAxis2 As New AxisItem()
            //DLxAxis2.axisNumber = AxisNumber.X_Axis
            //DLxAxis2.DataType = AxisDataType.String
            //DLxAxis2.Labels.ItemFormatString = "<ITEM_LABEL>"
            //DLxAxis2.Labels.Font = New Font("Tahoma", 7)
            //DLxAxis2.LineThickness = 1
            // Create an Y axis
            AxisItem DLyAxis1 = new AxisItem();
            DLyAxis1.axisNumber = AxisNumber.Y_Axis;
            DLyAxis1.Labels.ItemFormatString = "<DATA_VALUE:0.00#>";
            DLyAxis1.Labels.Font = new Font("Tahoma", 7);
            DLyAxis1.LineThickness = 1;
            AxisItem DLyAxis2 = new AxisItem();
            DLyAxis2.axisNumber = AxisNumber.Y2_Axis;
            DLyAxis2.Labels.ItemFormatString = "<DATA_VALUE:0.00#>";
            DLyAxis2.Labels.Font = new Font("Tahoma", 7);
            DLyAxis2.LineThickness = 1;
            myDLChartArea1.Axes.Add(DLxAxis1);
            myDLChartArea1.Axes.Add(DLyAxis1);
            //myDLChartArea1.Axes.Add(DLxAxis1)
            myDLChartArea1.Axes.Add(DLyAxis2);
            // Set the axes
            DLchartLayer1.AxisX = DLxAxis1;
            DLchartLayer1.AxisY = DLyAxis1;
            DLchartLayer2.AxisX = DLxAxis1;
            DLchartLayer2.AxisY = DLyAxis2;
            // Set the ChartArea
            DLchartLayer1.ChartArea = myDLChartArea1;
            DLchartLayer2.ChartArea = myDLChartArea1;
            // Add the ChartLayer to the ChartLayers collection
            MyDateLinechart1.CompositeChart.ChartLayers.Add(DLchartLayer1);
            MyDateLinechart1.CompositeChart.ChartLayers.Add(DLchartLayer2);
            ///''''''''''''''''''
            ISeries DLseries1 = null;
            NumericTimeSeries[] numericTimeSeries1 = new NumericTimeSeries[10];
            int Bcnt = 0;
            //check if we have a channel config else use default 69
            int myChannelConfig = 69;
            // LiveMonitoring.IRemoteLib.MeteringProfileMarker MyDataMarkerHistory = default(LiveMonitoring.IRemoteLib.MeteringProfileMarker);
            foreach (LiveMonitoring.IRemoteLib.MeteringProfileMarker MyDataMarkerHistory in MyDataMarkers)
            {
                if (MyDataMarkerHistory.ProfileMarkerType == 1 | MyDataMarkerHistory.ProfileMarkerType == 4)
                {
                    myChannelConfig = MyDataMarkerHistory.TRegisterConfigValue;
                }
            }
            //ok now we have channels set the names
            LiveMonitoring.IRemoteLib.ElsterA1700Data.TChannelConfig MyDataChannels = new LiveMonitoring.IRemoteLib.ElsterA1700Data.TChannelConfig(myChannelConfig);
            //Dim mychanel As LiveMonitoring.IRemoteLib.ElsterA1700Data.ChanelData

            //For Each mychanel In MyDataChannels.ChannelNames

            //Next
            double[] ConversionFactor = new double[10];
            int findMVA = 0;
            for (Bcnt = 0; Bcnt <= 7; Bcnt++)
            {
                numericTimeSeries1[Bcnt] = new NumericTimeSeries();
                if (MyDataChannels.ChannelNames.Contains((Bcnt).ToString()) == true)
                {
                    //If CType(SensorDet.Fields((Bcnt + 1).ToString), LiveMonitoring.IRemoteLib.SensorFieldsDef).DisplayValue Then
                    numericTimeSeries1[Bcnt] = new NumericTimeSeries();
                    numericTimeSeries1[Bcnt].Label = ((LiveMonitoring.IRemoteLib.ElsterA1700Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelName;
                    if (numericTimeSeries1[Bcnt].Label == "KVA")
                    {
                        findMVA = Bcnt + 1;
                    }
                    ConversionFactor[Bcnt] = ((LiveMonitoring.IRemoteLib.ElsterA1700Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelDivisorUnits;
                    //End If
                }
                else
                {
                    numericTimeSeries1[Bcnt] = new NumericTimeSeries();
                    numericTimeSeries1[Bcnt].Label = "";
                    ConversionFactor[Bcnt] = 0;
                }
            }
            //pf possible
            if (MyDataChannels.Import_mW & MyDataChannels.mVA)
            {
                numericTimeSeries1[8] = new NumericTimeSeries();
                numericTimeSeries1[8].Label = "Power Factor";
                ConversionFactor[8] = 0;
            }
            int tmp1cntwe = 0;
            //mycnt1= fields ?
            // LiveMonitoring.IRemoteLib.MeteringProfileRecord MyDataHistory = default(LiveMonitoring.IRemoteLib.MeteringProfileRecord);
            int MaxFieldCnt = 0;
            bool firstrec = true;
            foreach (LiveMonitoring.IRemoteLib.MeteringProfileRecord MyDataHistory in MyData)
            {
                int myret = 0;
                // Dim ConversionFactor As Double = 0
                try
                {
                    if (firstrec)
                    {
                        firstrec = false;
                        txtStart.Text = MyDataHistory.TimeStamp.ToLongDateString();

                    }

                }
                catch (Exception ex)
                {
                }
                try
                {
                    txtEnd.Text = MyDataHistory.TimeStamp.ToLongDateString();

                }
                catch (Exception ex)
                {
                }
                try
                {
                    myret = numericTimeSeries1[0].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[0].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    if (ConversionFactor[0] == 0)
                    {
                        numericTimeSeries1[0].Points[myret].NumericValue = MyDataHistory.Channel1;
                    }
                    else
                    {
                        numericTimeSeries1[0].Points[myret].NumericValue = MyDataHistory.Channel1 / ConversionFactor[0];
                    }
                    myret = numericTimeSeries1[1].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[1].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    //numericTimeSeries1(1).Points(myret).NumericValue = MyDataHistory.Channel2
                    if (ConversionFactor[1] == 0)
                    {
                        numericTimeSeries1[1].Points[myret].NumericValue = MyDataHistory.Channel2;
                    }
                    else
                    {
                        numericTimeSeries1[1].Points[myret].NumericValue = MyDataHistory.Channel2 / ConversionFactor[1];
                    }

                    myret = numericTimeSeries1[2].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[2].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    // numericTimeSeries1(2).Points(myret).NumericValue = MyDataHistory.Channel3
                    if (ConversionFactor[2] == 0)
                    {
                        numericTimeSeries1[2].Points[myret].NumericValue = MyDataHistory.Channel3;
                    }
                    else
                    {
                        numericTimeSeries1[2].Points[myret].NumericValue = MyDataHistory.Channel3 / ConversionFactor[2];
                    }

                    myret = numericTimeSeries1[3].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[3].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    //numericTimeSeries1(3).Points(myret).NumericValue = MyDataHistory.Channel4
                    if (ConversionFactor[3] == 0)
                    {
                        numericTimeSeries1[3].Points[myret].NumericValue = MyDataHistory.Channel4;
                    }
                    else
                    {
                        numericTimeSeries1[3].Points[myret].NumericValue = MyDataHistory.Channel4 / ConversionFactor[3];
                    }

                    myret = numericTimeSeries1[4].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[4].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    // numericTimeSeries1(4).Points(myret).NumericValue = MyDataHistory.Channel5
                    if (ConversionFactor[4] == 0)
                    {
                        numericTimeSeries1[4].Points[myret].NumericValue = MyDataHistory.Channel5;
                    }
                    else
                    {
                        numericTimeSeries1[4].Points[myret].NumericValue = MyDataHistory.Channel5 / ConversionFactor[4];
                    }

                    myret = numericTimeSeries1[5].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[5].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    // numericTimeSeries1(5).Points(myret).NumericValue = MyDataHistory.Channel6
                    if (ConversionFactor[5] == 0)
                    {
                        numericTimeSeries1[5].Points[myret].NumericValue = MyDataHistory.Channel6;
                    }
                    else
                    {
                        numericTimeSeries1[5].Points[myret].NumericValue = MyDataHistory.Channel6 / ConversionFactor[5];
                    }

                    myret = numericTimeSeries1[6].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[6].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    //numericTimeSeries1(6).Points(myret).NumericValue = MyDataHistory.Channel7
                    if (ConversionFactor[6] == 0)
                    {
                        numericTimeSeries1[6].Points[myret].NumericValue = MyDataHistory.Channel7;
                    }
                    else
                    {
                        numericTimeSeries1[6].Points[myret].NumericValue = MyDataHistory.Channel7 / ConversionFactor[6];
                    }

                    myret = numericTimeSeries1[7].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[7].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    // numericTimeSeries1(7).Points(myret).NumericValue = MyDataHistory.Channel8
                    if (ConversionFactor[7] == 0)
                    {
                        numericTimeSeries1[7].Points[myret].NumericValue = MyDataHistory.Channel8;
                    }
                    else
                    {
                        numericTimeSeries1[7].Points[myret].NumericValue = MyDataHistory.Channel8 / ConversionFactor[7];
                    }
                    //pf possible
                    if (MyDataChannels.Import_mW & MyDataChannels.mVA)
                    {
                        double mwPF = MyDataHistory.Channel1;
                        double mVAPF = 0;
                        switch (findMVA)
                        {
                            case 1:
                                mVAPF = MyDataHistory.Channel1;
                                break;
                            case 2:
                                mVAPF = MyDataHistory.Channel2;
                                break;
                            case 3:
                                mVAPF = MyDataHistory.Channel3;
                                break;
                            case 4:
                                mVAPF = MyDataHistory.Channel4;
                                break;
                            case 5:
                                mVAPF = MyDataHistory.Channel5;
                                break;
                            case 6:
                                mVAPF = MyDataHistory.Channel6;
                                break;
                            case 7:
                                mVAPF = MyDataHistory.Channel7;
                                break;
                            case 8:
                                mVAPF = MyDataHistory.Channel8;
                                break;
                            default:
                                mVAPF = 0;
                                break;
                        }
                        if (mVAPF > 0)
                        {
                            myret = numericTimeSeries1[8].Points.Add(new NumericTimeDataPoint());
                            numericTimeSeries1[8].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                            numericTimeSeries1[8].Points[myret].NumericValue = (mwPF / mVAPF);
                        }
                        else
                        {
                            myret = numericTimeSeries1[8].Points.Add(new NumericTimeDataPoint());
                            numericTimeSeries1[8].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                            numericTimeSeries1[8].Points[myret].NumericValue = 0;

                        }
                    }

                }
                catch (Exception ex)
                {
                }

                // End If
            }

            int Acnt = 0;
            for (Acnt = 0; Acnt <= 7; Acnt++)
            {
                if (MyDataChannels.ChannelNames.Contains((Acnt).ToString()) == true)
                {
                    DLchartLayer1.Series.Add(numericTimeSeries1[Acnt]);
                    DLseries1 = numericTimeSeries1[Acnt];
                    MyDateLinechart1.Series.Add(DLseries1);
                }
            }
            //pf
            if (MyDataChannels.Import_mW & MyDataChannels.mVA)
            {
                DLchartLayer2.Series.Add(numericTimeSeries1[8]);
                DLseries1 = numericTimeSeries1[8];
                MyDateLinechart1.Series.Add(DLseries1);

            }
            ///''''''''''''''''''
            // Set X axis
            DLxAxis1.Labels.Orientation = TextOrientation.VerticalLeftFacing;
            DLxAxis1.Labels.ItemFormatString = "<ITEM_LABEL:MM/dd/yyyy HH:mm>";
            //DLxAxis2.Labels.Orientation = TextOrientation.VerticalLeftFacing
            //DLxAxis2.Labels.ItemFormatString = "<ITEM_LABEL:MM/dd/yyyy HH:mm>"
            // Set Y axis
            // Set the ChartType
            DLchartLayer1.ChartType = ChartType.LineChart;
            DLchartLayer2.ChartType = ChartType.LineChart;
            // Set Axis Type
            SetAxisTypes(DLchartLayer1);
            SetAxisTypes(DLchartLayer2);
            //DLchartLayer1.AxisY.Extent = 130
            //Dim atype As New AxisDataType
            DLchartLayer1.AxisX.DataType = AxisDataType.Time;
            DLchartLayer1.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
            DLchartLayer1.AxisY.DataType = AxisDataType.Numeric;

            DLchartLayer2.AxisX.DataType = AxisDataType.Time;
            DLchartLayer2.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
            DLchartLayer2.AxisY.DataType = AxisDataType.Numeric;
            // Add the series to the ChartLayer's Series collection.
            MyDateLinechart1.Width = 1200;
            MyDateLinechart1.Height = 400;
            CompositeLegend legend1 = new CompositeLegend();
            legend1.LabelStyle.Font = new Font("Times New Roman", 10);
            MyDateLinechart1.CompositeChart.Legends.Add(legend1);
            //MyDateLinechart1.CompositeChart.ChartLayers(0).ChartLayer.LayerID = "1"
            // MyDateLinechart1.CompositeChart.ChartLayers(1).ChartLayer.LayerID = "2"
            legend1.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers[0]);
            legend1.BoundsMeasureType = MeasureType.Percentage;
            legend1.Bounds = new Rectangle(1, 1, 10, 10);
            CompositeLegend legend2 = new CompositeLegend();
            legend2.LabelStyle.Font = new Font("Times New Roman", 10);
            MyDateLinechart1.CompositeChart.Legends.Add(legend2);
            legend2.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers[1]);
            legend2.BoundsMeasureType = MeasureType.Percentage;
            legend2.Bounds = new Rectangle(88, 1, 11, 7);
            //right,top,width,height
            MyDateLinechart1.CompositeChart.ChartAreas[0].BoundsMeasureType = MeasureType.Percentage;
            MyDateLinechart1.CompositeChart.ChartAreas[0].Bounds = new Rectangle(5, 0, 90, 100);
            Infragistics.UltraChart.Resources.Appearance.BorderAppearance MyBorder = new Infragistics.UltraChart.Resources.Appearance.BorderAppearance();
            MyBorder.Thickness = 0;
            MyDateLinechart1.CompositeChart.ChartAreas[0].Border = MyBorder;
            //MyDateLinechart1.CompositeChart.ChartAreas(1).BoundsMeasureType = MeasureType.Percentage
            //MyDateLinechart1.CompositeChart.ChartAreas(1).Bounds = New Rectangle(0, 20, 100, 80)
            this.Charts.Controls.Add(MyDateLinechart1);
            //Dim legend1 As New CompositeLegend()
            //legend1.LabelStyle.Font = New Font("Times New Roman", 10)
            //MyDateLinechart1.CompositeChart.Legends.Add(legend1)
            //legend1.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers(0))
            //legend1.BoundsMeasureType = MeasureType.Percentage
            // legend1.Bounds = New Rectangle(5, 5, 105, 14)
            if (GeneratePDF)
            {
                MemoryStream ms = new MemoryStream();
                //= New MemoryStream(myImageBytes, 0, myImageBytes.Length)
                MyDateLinechart1.SaveTo(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                System.Drawing.Image lastimage = default(System.Drawing.Image);
                lastimage = System.Drawing.Image.FromStream(ms);
                lastimage.Tag = SensorDet.Caption;
                Rep2Images.Add(lastimage, MyDateLinechart1.ID);
            }
        }
        private void ultraChart1_ChartDrawItem(object sender, ChartDrawItemEventArgs e)
        {
            try
            {
                if ((e.Primitive == null) == false)
                {
                    if ((e.Primitive.Path == null) == false)
                    {
                        if (e.Primitive.Path.IndexOf("Legend") != -1)
                        {
                            if (e.Primitive.Path.IndexOf("Border") == -1)
                            {
                                e.Primitive.PE.StrokeWidth = 4;
                            }
                        }
                    }
                }
                //If TypeOf e.Primitive.Layer Is Infragistics.UltraChart.Core.Layers.LineLayer Then
                //    e.Primitive.PE.Fill = System.Drawing.Color.Red

                //End If
                //If IsNothing(e.Primitive.Layer) = False Then
                //    e.Primitive.PE.Fill = System.Drawing.Color.Blue

                //End If
                //If (TypeOf e.Primitive Is Infragistics.UltraChart.Core.Primitives.Line) And TypeOf e.Primitive.Layer Is Infragistics.UltraChart.Core.Layers.LineLayer Then
                //    e.Primitive.PE.Fill = System.Drawing.Color.Pink
                //    'Else
                //    '    Select Case e.Primitive.Column
                //    '        Case 0
                //    '            e.Primitive.PE.Fill = System.Drawing.Color.Yellow
                //    '        Case 1
                //    '            e.Primitive.PE.Fill = System.Drawing.Color.Blue
                //    '        Case 2
                //    '            e.Primitive.PE.Fill = System.Drawing.Color.Red
                //    '    End Select
                //End If
            }
            catch (Exception ex)
            {
                Trace.Write("err" + ex.Message);
            }

        }
        //Handles UltraChart1.ChartDrawItem
        private void MyDateLinechart1_ChartDrawItem1(object sender, Infragistics.UltraChart.Shared.Events.ChartDrawItemEventArgs e)
        {
            if ((object.ReferenceEquals(e.Primitive.GetType(), typeof(Infragistics.UltraChart.Core.Primitives.Polyline))) & object.ReferenceEquals(e.Primitive.Layer, typeof(Infragistics.UltraChart.Core.Layers.LineLayer)))
            {
                e.Primitive.PE.Fill = System.Drawing.Color.Red;
                //Else
                //    Select Case e.Primitive.Column
                //        Case 0
                //            e.Primitive.PE.Fill = System.Drawing.Color.Yellow
                //        Case 1
                //            e.Primitive.PE.Fill = System.Drawing.Color.Blue
                //        Case 2
                //            e.Primitive.PE.Fill = System.Drawing.Color.Red
                //    End Select
            }

        }


        public void DrawElsterA1140ProfileGraphs(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringProfileRecord> MyData, List<LiveMonitoring.IRemoteLib.MeteringProfileMarker> MyDataMarkers)
        {
            try
            {
                DrawElsterA1140ProfileGraphs2(SensorDet, MyData, MyDataMarkers);
                //Active Power
                AddPageBreak();

            }
            catch (Exception ex)
            {
            }



        }

        public void DrawElsterA1140ProfileGraphs2(LiveMonitoring.IRemoteLib.SensorDetails SensorDet, List<LiveMonitoring.IRemoteLib.MeteringProfileRecord> MyData, List<LiveMonitoring.IRemoteLib.MeteringProfileMarker> MyDataMarkers)
        {
            Infragistics.WebUI.UltraWebChart.UltraChart MyDateLinechart1 = new Infragistics.WebUI.UltraWebChart.UltraChart();
            //attach event for legent width
           // MyDateLinechart1.EnableCrossHair = this.CheckBox1.Checked;

            MyDateLinechart1.ChartDrawItem += ultraChart1_ChartDrawItem;
            MyDateLinechart1.ID = "ElsterA1140PG" + SensorDet.ID.ToString();
            MyDateLinechart1.TitleTop.Extent = 45;
            //sets the font color
            MyDateLinechart1.TitleTop.FontColor = Color.Red;
            //sets whether the chart auto-sizes the font for the title
            //MyDateLinechart1.TitleTop.FontSizeBestFit = false
            //sets the horizontal alignment of the text
            MyDateLinechart1.TitleTop.HorizontalAlign = StringAlignment.Center;
            //sets the margins for the Top, Bottom, Left and right
            MyDateLinechart1.TitleTop.Margins.Bottom = 2;
            MyDateLinechart1.TitleTop.Margins.Top = 2;
            MyDateLinechart1.TitleTop.Margins.Left = 2;
            MyDateLinechart1.TitleTop.Margins.Right = 2;
            //sets the text to display for the chart in the title
            MyDateLinechart1.TitleTop.Text = "Elster Profile Graphs :" + SensorDet.Caption;
            //sets the vertical alignment of the title
            MyDateLinechart1.TitleTop.VerticalAlign = StringAlignment.Near;
            //show/hide the referenced title
            MyDateLinechart1.TitleTop.Visible = true;
            //wrap/don't wrap the text
            MyDateLinechart1.TitleTop.WrapText = true;
            // Set composite charts
            MyDateLinechart1.ChartType = ChartType.Composite;
            // Create the ChartArea
            ChartArea myDLChartArea1 = new ChartArea();
            //Dim myDLChartArea2 As New ChartArea()
            // Add the Chart Area to the ChartAreas collection
            MyDateLinechart1.CompositeChart.ChartAreas.Add(myDLChartArea1);
            //MyDateLinechart1.CompositeChart.ChartAreas.Add(myDLChartArea2)
            // Create the ChartLayer
            ChartLayerAppearance DLchartLayer1 = new ChartLayerAppearance();
            ChartLayerAppearance DLchartLayer2 = new ChartLayerAppearance();
            // Set the ChartType
            DLchartLayer1.ChartType = ChartType.LineChart;
            DLchartLayer2.ChartType = ChartType.LineChart;
            // Create an X axis
            AxisItem DLxAxis1 = new AxisItem();
            DLxAxis1.axisNumber = AxisNumber.X_Axis;
            DLxAxis1.DataType = AxisDataType.String;
            DLxAxis1.Labels.ItemFormatString = "<ITEM_LABEL>";
            DLxAxis1.Labels.Font = new Font("Tahoma", 7);
            DLxAxis1.LineThickness = 1;
            //Dim DLxAxis2 As New AxisItem()
            //DLxAxis2.axisNumber = AxisNumber.X_Axis
            //DLxAxis2.DataType = AxisDataType.String
            //DLxAxis2.Labels.ItemFormatString = "<ITEM_LABEL>"
            //DLxAxis2.Labels.Font = New Font("Tahoma", 7)
            //DLxAxis2.LineThickness = 1
            // Create an Y axis
            AxisItem DLyAxis1 = new AxisItem();
            DLyAxis1.axisNumber = AxisNumber.Y_Axis;
            DLyAxis1.Labels.ItemFormatString = "<DATA_VALUE:0.00#>";
            DLyAxis1.Labels.Font = new Font("Tahoma", 7);
            DLyAxis1.LineThickness = 1;
            AxisItem DLyAxis2 = new AxisItem();
            DLyAxis2.axisNumber = AxisNumber.Y2_Axis;
            DLyAxis2.Labels.ItemFormatString = "<DATA_VALUE:0.00#>";
            DLyAxis2.Labels.Font = new Font("Tahoma", 7);
            DLyAxis2.LineThickness = 1;
            myDLChartArea1.Axes.Add(DLxAxis1);
            myDLChartArea1.Axes.Add(DLyAxis1);
            //myDLChartArea1.Axes.Add(DLxAxis1)
            myDLChartArea1.Axes.Add(DLyAxis2);
            // Set the axes
            DLchartLayer1.AxisX = DLxAxis1;
            DLchartLayer1.AxisY = DLyAxis1;
            DLchartLayer2.AxisX = DLxAxis1;
            DLchartLayer2.AxisY = DLyAxis2;
            // Set the ChartArea
            DLchartLayer1.ChartArea = myDLChartArea1;
            DLchartLayer2.ChartArea = myDLChartArea1;
            // Add the ChartLayer to the ChartLayers collection
            MyDateLinechart1.CompositeChart.ChartLayers.Add(DLchartLayer1);
            MyDateLinechart1.CompositeChart.ChartLayers.Add(DLchartLayer2);
            ///''''''''''''''''''
            ISeries DLseries1 = null;
            NumericTimeSeries[] numericTimeSeries1 = new NumericTimeSeries[9];
            int Bcnt = 0;
            //check if we have a channel config else use default 69
            int myChannelConfig = 69;
            //LiveMonitoring.IRemoteLib.MeteringProfileMarker MyDataMarkerHistory = default(LiveMonitoring.IRemoteLib.MeteringProfileMarker);
            foreach (LiveMonitoring.IRemoteLib.MeteringProfileMarker MyDataMarkerHistory in MyDataMarkers)
            {
                if (MyDataMarkerHistory.ProfileMarkerType == 1 | MyDataMarkerHistory.ProfileMarkerType == 4)
                {
                    myChannelConfig = MyDataMarkerHistory.TRegisterConfigValue;
                }
            }
            //ok now we have channels set the names
            LiveMonitoring.IRemoteLib.ElsterA1140Data.TChannelConfig MyDataChannels = new LiveMonitoring.IRemoteLib.ElsterA1140Data.TChannelConfig(myChannelConfig);
            //Dim mychanel As LiveMonitoring.IRemoteLib.ElsterA1140Data.ChanelData

            //For Each mychanel In MyDataChannels.ChannelNames

            //Next
            double[] ConversionFactor = new double[10];
            int findMVA = 0;
            for (Bcnt = 0; Bcnt <= 7; Bcnt++)
            {
                numericTimeSeries1[Bcnt] = new NumericTimeSeries();
                if (MyDataChannels.ChannelNames.Contains((Bcnt).ToString()) == true)
                {
                    //If CType(SensorDet.Fields((Bcnt + 1).ToString), LiveMonitoring.IRemoteLib.SensorFieldsDef).DisplayValue Then
                    numericTimeSeries1[Bcnt] = new NumericTimeSeries();
                    numericTimeSeries1[Bcnt].Label = ((LiveMonitoring.IRemoteLib.ElsterA1140Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelName;
                    if (numericTimeSeries1[Bcnt].Label == "KVA")
                    {
                        findMVA = Bcnt + 1;
                    }
                    ConversionFactor[Bcnt] = ((LiveMonitoring.IRemoteLib.ElsterA1140Data.ChanelData)MyDataChannels.ChannelNames[Convert.ToString(Bcnt)]).ChannelDivisorUnits;
                    //End If
                }
                else
                {
                    numericTimeSeries1[Bcnt] = new NumericTimeSeries();
                    numericTimeSeries1[Bcnt].Label = "";
                    ConversionFactor[Bcnt] = 0;
                }
            }
            //pf possible
            if (MyDataChannels.Import_mW & MyDataChannels.mVA)
            {
                numericTimeSeries1[8] = new NumericTimeSeries();
                numericTimeSeries1[8].Label = "Power Factor";
                ConversionFactor[8] = 0;
            }
            int tmp1cntwe = 0;
            //mycnt1= fields ?
            //   LiveMonitoring.IRemoteLib.MeteringProfileRecord MyDataHistory = default(LiveMonitoring.IRemoteLib.MeteringProfileRecord);
            int MaxFieldCnt = 0;
            foreach (LiveMonitoring.IRemoteLib.MeteringProfileRecord MyDataHistory in MyData)
            {
                int myret = 0;
                // Dim ConversionFactor As Double = 0
                try
                {
                    myret = numericTimeSeries1[0].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[0].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    if (ConversionFactor[0] == 0)
                    {
                        numericTimeSeries1[0].Points[myret].NumericValue = MyDataHistory.Channel1;
                    }
                    else
                    {
                        numericTimeSeries1[0].Points[myret].NumericValue = MyDataHistory.Channel1 / ConversionFactor[0];
                    }
                    myret = numericTimeSeries1[1].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[1].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    //numericTimeSeries1(1).Points(myret).NumericValue = MyDataHistory.Channel2
                    if (ConversionFactor[1] == 0)
                    {
                        numericTimeSeries1[1].Points[myret].NumericValue = MyDataHistory.Channel2;
                    }
                    else
                    {
                        numericTimeSeries1[1].Points[myret].NumericValue = MyDataHistory.Channel2 / ConversionFactor[1];
                    }

                    myret = numericTimeSeries1[2].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[2].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    // numericTimeSeries1(2).Points(myret).NumericValue = MyDataHistory.Channel3
                    if (ConversionFactor[2] == 0)
                    {
                        numericTimeSeries1[2].Points[myret].NumericValue = MyDataHistory.Channel3;
                    }
                    else
                    {
                        numericTimeSeries1[2].Points[myret].NumericValue = MyDataHistory.Channel3 / ConversionFactor[2];
                    }

                    myret = numericTimeSeries1[3].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[3].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    //numericTimeSeries1(3).Points(myret).NumericValue = MyDataHistory.Channel4
                    if (ConversionFactor[3] == 0)
                    {
                        numericTimeSeries1[3].Points[myret].NumericValue = MyDataHistory.Channel4;
                    }
                    else
                    {
                        numericTimeSeries1[3].Points[myret].NumericValue = MyDataHistory.Channel4 / ConversionFactor[3];
                    }

                    myret = numericTimeSeries1[4].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[4].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    // numericTimeSeries1(4).Points(myret).NumericValue = MyDataHistory.Channel5
                    if (ConversionFactor[4] == 0)
                    {
                        numericTimeSeries1[4].Points[myret].NumericValue = MyDataHistory.Channel5;
                    }
                    else
                    {
                        numericTimeSeries1[4].Points[myret].NumericValue = MyDataHistory.Channel5 / ConversionFactor[4];
                    }

                    myret = numericTimeSeries1[5].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[5].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    // numericTimeSeries1(5).Points(myret).NumericValue = MyDataHistory.Channel6
                    if (ConversionFactor[5] == 0)
                    {
                        numericTimeSeries1[5].Points[myret].NumericValue = MyDataHistory.Channel6;
                    }
                    else
                    {
                        numericTimeSeries1[5].Points[myret].NumericValue = MyDataHistory.Channel6 / ConversionFactor[5];
                    }

                    myret = numericTimeSeries1[6].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[6].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    //numericTimeSeries1(6).Points(myret).NumericValue = MyDataHistory.Channel7
                    if (ConversionFactor[6] == 0)
                    {
                        numericTimeSeries1[6].Points[myret].NumericValue = MyDataHistory.Channel7;
                    }
                    else
                    {
                        numericTimeSeries1[6].Points[myret].NumericValue = MyDataHistory.Channel7 / ConversionFactor[6];
                    }

                    myret = numericTimeSeries1[7].Points.Add(new NumericTimeDataPoint());
                    numericTimeSeries1[7].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                    // numericTimeSeries1(7).Points(myret).NumericValue = MyDataHistory.Channel8
                    if (ConversionFactor[7] == 0)
                    {
                        numericTimeSeries1[7].Points[myret].NumericValue = MyDataHistory.Channel8;
                    }
                    else
                    {
                        numericTimeSeries1[7].Points[myret].NumericValue = MyDataHistory.Channel8 / ConversionFactor[7];
                    }
                    //pf possible
                    if (MyDataChannels.Import_mW & MyDataChannels.mVA)
                    {
                        double mwPF = MyDataHistory.Channel1;
                        double mVAPF = 0;
                        switch (findMVA)
                        {
                            case 1:
                                mVAPF = MyDataHistory.Channel1;
                                break;
                            case 2:
                                mVAPF = MyDataHistory.Channel2;
                                break;
                            case 3:
                                mVAPF = MyDataHistory.Channel3;
                                break;
                            case 4:
                                mVAPF = MyDataHistory.Channel4;
                                break;
                            case 5:
                                mVAPF = MyDataHistory.Channel5;
                                break;
                            case 6:
                                mVAPF = MyDataHistory.Channel6;
                                break;
                            case 7:
                                mVAPF = MyDataHistory.Channel7;
                                break;
                            case 8:
                                mVAPF = MyDataHistory.Channel8;
                                break;
                            default:
                                mVAPF = 0;
                                break;
                        }
                        if (mVAPF > 0)
                        {
                            myret = numericTimeSeries1[8].Points.Add(new NumericTimeDataPoint());
                            numericTimeSeries1[8].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                            numericTimeSeries1[8].Points[myret].NumericValue = (mwPF / mVAPF);
                        }
                        else
                        {
                            myret = numericTimeSeries1[8].Points.Add(new NumericTimeDataPoint());
                            numericTimeSeries1[8].Points[myret].TimeValue = MyDataHistory.TimeStamp;
                            numericTimeSeries1[8].Points[myret].NumericValue = 0;

                        }
                    }

                }
                catch (Exception ex)
                {
                }

                // End If
            }
            int Acnt = 0;
            for (Acnt = 0; Acnt <= 7; Acnt++)
            {
                if (MyDataChannels.ChannelNames.Contains((Acnt).ToString()) == true)
                {
                    DLchartLayer1.Series.Add(numericTimeSeries1[Acnt]);
                    DLseries1 = numericTimeSeries1[Acnt];
                    MyDateLinechart1.Series.Add(DLseries1);
                }
            }
            //pf
            if (MyDataChannels.Import_mW & MyDataChannels.mVA)
            {
                DLchartLayer2.Series.Add(numericTimeSeries1[8]);
                DLseries1 = numericTimeSeries1[8];
                MyDateLinechart1.Series.Add(DLseries1);
                //Dim MyPaintElement As New PaintElement(Color.DarkRed, Color.DarkRed)
                // MyPaintElement.Fill = Color.DarkRed
                //MyPaintElement.Stroke = Color.DarkRed
                //MyPaintElement.StrokeWidth = 2
                //numericTimeSeries1(8).PEs = MyPaintElement
            }
            ///''''''''''''''''''
            // Set X axis
            DLxAxis1.Labels.Orientation = TextOrientation.VerticalLeftFacing;
            DLxAxis1.Labels.ItemFormatString = "<ITEM_LABEL:MM/dd/yyyy HH:mm>";
            //DLxAxis2.Labels.Orientation = TextOrientation.VerticalLeftFacing
            //DLxAxis2.Labels.ItemFormatString = "<ITEM_LABEL:MM/dd/yyyy HH:mm>"
            // Set Y axis
            // Set the ChartType
            DLchartLayer1.ChartType = ChartType.LineChart;
            DLchartLayer2.ChartType = ChartType.LineChart;
            // Set Axis Type
            SetAxisTypes(DLchartLayer1);
            SetAxisTypes(DLchartLayer2);
            //DLchartLayer1.AxisY.Extent = 130
            //Dim atype As New AxisDataType
            DLchartLayer1.AxisX.DataType = AxisDataType.Time;
            DLchartLayer1.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
            DLchartLayer1.AxisY.DataType = AxisDataType.Numeric;

            DLchartLayer2.AxisX.DataType = AxisDataType.Time;
            DLchartLayer2.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
            DLchartLayer2.AxisY.DataType = AxisDataType.Numeric;
            // Add the series to the ChartLayer's Series collection.
            MyDateLinechart1.Width = 700;
            MyDateLinechart1.Height = 500;
            CompositeLegend legend1 = new CompositeLegend();
            legend1.LabelStyle.Font = new Font("Times New Roman", 10);
            MyDateLinechart1.CompositeChart.Legends.Add(legend1);
            legend1.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers[0]);
            legend1.BoundsMeasureType = MeasureType.Percentage;
            legend1.Bounds = new Rectangle(5, 5, 40, 14);
            CompositeLegend legend2 = new CompositeLegend();
            legend2.LabelStyle.Font = new Font("Times New Roman", 10);
            MyDateLinechart1.CompositeChart.Legends.Add(legend2);
            legend2.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers[1]);
            legend2.BoundsMeasureType = MeasureType.Percentage;
            legend2.Bounds = new Rectangle(45, 5, 40, 14);
            MyDateLinechart1.CompositeChart.ChartAreas[0].BoundsMeasureType = MeasureType.Percentage;
            MyDateLinechart1.CompositeChart.ChartAreas[0].Bounds = new Rectangle(0, 20, 100, 80);

            Color[] ChartColors4 = null;
            ChartColors4 = new Color[] {
                Color.Orange,
                Color.Yellow,
                Color.Blue,
                Color.Green,
                Color.Red,
                Color.Black,
                Color.Blue,
                Color.Aqua,
                Color.Bisque,
                Color.Gold,
                Color.OrangeRed
            };
            MyDateLinechart1.ColorModel.CustomPalette = ChartColors4;
            //DLchartLayer2.c
            //MyDateLinechart1.CompositeChart.ChartAreas(1).BoundsMeasureType = MeasureType.Percentage
            //MyDateLinechart1.CompositeChart.ChartAreas(1).Bounds = New Rectangle(0, 20, 100, 80)
            this.Charts.Controls.Add(MyDateLinechart1);
            //Dim legend1 As New CompositeLegend()
            //legend1.LabelStyle.Font = New Font("Times New Roman", 10)
            //MyDateLinechart1.CompositeChart.Legends.Add(legend1);
            //legend1.ChartLayers.Add(MyDateLinechart1.CompositeChart.ChartLayers(0));
            //legend1.BoundsMeasureType = MeasureType.Percentage;
            // legend1.Bounds = New Rectangle(5, 5, 105, 14);
            if (GeneratePDF)
            {
                MemoryStream ms = new MemoryStream();
                //= New MemoryStream(myImageBytes, 0, myImageBytes.Length)
                MyDateLinechart1.SaveTo(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                System.Drawing.Image lastimage = default(System.Drawing.Image);
                lastimage = System.Drawing.Image.FromStream(ms);
                lastimage.Tag = SensorDet.Caption;
                Rep2Images.Add(lastimage, MyDateLinechart1.ID);
            }
        }


        private void SetAxisTypes(ChartLayerAppearance layer)
        {
            switch (layer.ChartType)
            {
                case ChartType.ColumnChart:
                case ChartType.StackColumnChart:
                    layer.AxisX.DataType = AxisDataType.String;
                    layer.AxisX.SetLabelAxisType = SetLabelAxisType.GroupBySeries;
                    layer.AxisY.DataType = AxisDataType.Numeric;
                    layer.AxisY.TickmarkStyle = AxisTickStyle.Smart;
                    break;
                case ChartType.BarChart:
                case ChartType.StackBarChart:
                    layer.AxisY.DataType = AxisDataType.String;
                    layer.AxisY.SetLabelAxisType = SetLabelAxisType.GroupBySeries;
                    layer.AxisX.DataType = AxisDataType.Numeric;
                    layer.AxisX.TickmarkStyle = AxisTickStyle.Smart;
                    layer.AxisX.SetLabelAxisType = SetLabelAxisType.ContinuousData;
                    layer.AxisX.Labels.ItemFormatString = "<DATA_VALUE:0.00#>";
                    break;
                case ChartType.LineChart:
                case ChartType.SplineChart:
                case ChartType.AreaChart:
                case ChartType.SplineAreaChart:
                case ChartType.StackLineChart:
                case ChartType.StackSplineChart:
                case ChartType.StackAreaChart:
                case ChartType.StackSplineAreaChart:
                    layer.AxisX.DataType = AxisDataType.String;
                    layer.AxisX.SetLabelAxisType = SetLabelAxisType.ContinuousData;
                    layer.AxisY.DataType = AxisDataType.Numeric;
                    layer.AxisY.TickmarkStyle = AxisTickStyle.Smart;
                    break;
                case ChartType.BubbleChart:
                case ChartType.ScatterChart:
                    layer.AxisX.DataType = AxisDataType.Numeric;
                    layer.AxisY.DataType = AxisDataType.Numeric;
                    layer.AxisX.TickmarkStyle = AxisTickStyle.Smart;
                    layer.AxisY.TickmarkStyle = AxisTickStyle.Smart;
                    break;
                case ChartType.RadarChart:
                case ChartType.PieChart:
                case ChartType.DoughnutChart:
                case ChartType.PolarChart:
                    break;
                case ChartType.CandleChart:
                    layer.AxisX.DataType = AxisDataType.String;
                    layer.AxisX.SetLabelAxisType = SetLabelAxisType.DateData;
                    layer.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
                    layer.AxisY.DataType = AxisDataType.Numeric;
                    layer.AxisY2.DataType = AxisDataType.Numeric;
                    layer.AxisY.TickmarkStyle = AxisTickStyle.Smart;
                    layer.AxisY2.TickmarkStyle = AxisTickStyle.Smart;
                    break;
                case ChartType.ParetoChart:
                    // Create an Y2 axis
                    AxisItem yAxis2 = new AxisItem();
                    yAxis2.axisNumber = AxisNumber.Y2_Axis;
                    yAxis2.Labels.ItemFormatString = "<DATA_VALUE:#0.0#>";
                    yAxis2.RangeType = AxisRangeType.Custom;
                    yAxis2.RangeMax = 1000;

                    // Add the second Y axes to the ChartArea
                    layer.ChartArea.Axes.Add(yAxis2);

                    // Set the second Y axes
                    layer.AxisY2 = yAxis2;

                    layer.AxisX.DataType = AxisDataType.String;
                    layer.AxisX.SetLabelAxisType = SetLabelAxisType.ContinuousData;
                    layer.AxisY.DataType = AxisDataType.Numeric;
                    layer.AxisY.TickmarkStyle = AxisTickStyle.Smart;
                    layer.AxisY2.DataType = AxisDataType.Numeric;
                    layer.AxisY2.TickmarkStyle = AxisTickStyle.Smart;
                    break;
                case ChartType.StepLineChart:
                case ChartType.StepAreaChart:
                    layer.AxisX.DataType = AxisDataType.Time;
                    layer.AxisY.DataType = AxisDataType.Numeric;
                    break;
                case ChartType.GanttChart:
                    layer.AxisX.DataType = AxisDataType.Time;
                    layer.AxisX.Labels.Orientation = TextOrientation.VerticalLeftFacing;
                    layer.AxisY.DataType = AxisDataType.String;
                    break;
            }

        }
        public void RegenerateCallbackGraphs()
        {
            this.Charts.Controls.Clear();
            FillSessionSensors();
            string[] mySensors = Strings.Split(Session["Sensors"].ToString(), ",");
            int Acnt = 0;
            LiveMonitoring.GlobalRemoteVars MyRem = new LiveMonitoring.GlobalRemoteVars();
            //If WebPanel1.Expanded Then
            //    WebPanel1.Expanded = False
            //End If
            //- 1 'last one is empty
            for (Acnt = 0; Acnt <= Information.UBound(mySensors); Acnt++)
            {
                Collection MyCollection = new Collection();
                MyCollection = MyRem.get_GetServerObjects(((Session["SelectedSite"] == null) ? null : Session["SelectedSite"]));
                //GetServerObjects 'server1.GetAll()
                object MyObject1 = null;
                int MyCnt = 0;
                foreach (object MyObject1_loopVariable in MyCollection)
                {
                    MyObject1 = MyObject1_loopVariable;
                    if (MyObject1 is LiveMonitoring.IRemoteLib.SensorDetails)
                    {
                        LiveMonitoring.IRemoteLib.SensorDetails MySensor = (LiveMonitoring.IRemoteLib.SensorDetails)MyObject1;
                        if (Convert.ToInt32(mySensors[Acnt]) == MySensor.ID)
                        {
                            AddLayer(MySensor);
                            AddPageBreak();
                            AddTarrifReport(MySensor);
                            break; // TODO: might not be correct. Was : Exit For

                        }
                        else
                        {
                            MyCnt += 1;
                        }
                    }
                }
                // AddImages(Acnt + 1, CInt(myCameras(Acnt)), 200, 200)
            }
        }

        // protected void btnGenerate2_Click(object sender, Infragistics.WebUI.WebDataInput.ButtonEventArgs e)
        protected void btnGenerate2_Click(object sender, EventArgs e)
        {
            lblErr.Visible = false;
            if (!string.IsNullOrEmpty(txtGraphs.Text))
            {
                GeneratePDF = true;
            }
            else
            {
                GeneratePDF = false;
            }
            switch (Convert.ToInt32(this.ddlRanges.SelectedValue))
            {
                case 0:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Minute, -30, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 1:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Hour, -1, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 2:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Hour, -2, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 3:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Hour, -3, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 4:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Hour, -5, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 5:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Hour, -10, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 6:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Hour, -12, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 7:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Hour, -24, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 8:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Day, -2, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 9:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Day, -4, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 10:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Day, -7, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
                case 11:
                    Session["StartDate"] = DateAndTime.DateAdd(DateInterval.Month, -1, DateAndTime.Now);
                    Session["EndDate"] = DateAndTime.Now;
                    break;
            }
            RegenerateCallbackGraphs();
            try
            {
                txtStart.Text = Session["StartDate"].ToString();
                txtEnd.Text = Session["EndDate"].ToString();

            }
            catch (Exception ex)
            {
            }
            if (GeneratePDF)
            {
                LiveMonitoring.DataAccess MyFunc = new LiveMonitoring.DataAccess();
               MyFunc.SendEmail(Rep2Images, txtGraphs.Text, MyFunc.GetAppSetting("OnlineReport.0.ReportName"), ExportStr.ToString(), MyFunc.GetAppSetting("OnlineReport.0.From"), Rep2HTML.ToString());
                //AddPDF()
            }
        }
       

        // protected void btnGenerate1_Click1(object sender, Infragistics.WebUI.WebDataInput.ButtonEventArgs e)
        protected void btnGenerate1_Click1(object sender, EventArgs e/*, Infragistics.WebUI.WebDataInput.ButtonEventArgs ie*/)
        {
            lblErr.Visible = false;
            //If startrange.Text = "" Or endrange.Text = "" Then
            //    lblErr.Visible = True
            //    lblErr.Text = "Error please select start/end date!"
            //    Exit Sub
            //End If
            Session["StartDate"] = txtStart.Text;
            Session["EndDate"] = txtEnd.Text;
            RegenerateCallbackGraphs();
            if (!string.IsNullOrEmpty(txtGraphs.Text))
            {
                GeneratePDF = true;
            }
            else
            {
                GeneratePDF = false;
            }
            if (string.IsNullOrEmpty(txtStart.Text) | string.IsNullOrEmpty(txtEnd.Text))
            {
            
                lblErr.Visible = true;
                lblErr.Text = "Error please select start/end date!";
                return;
            }
            //Session["StartDate"] = txtStart.Text;
            //Session["EndDate"] = txtEnd.Text;
            //RegenerateCallbackGraphs();
            if (GeneratePDF)
            {
                LiveMonitoring.DataAccess MyFunc = new LiveMonitoring.DataAccess();
              MyFunc.SendEmail(Rep2Images, txtGraphs.Text, MyFunc.GetAppSetting("OnlineReport.0.ReportName"), ExportStr.ToString(), MyFunc.GetAppSetting("OnlineReport.0.From"), Rep2HTML.ToString());
               // AddPDF()
            }
        }
        public void FillSessionSensors()
        {
            System.Web.UI.WebControls.ListItem MyItem = new System.Web.UI.WebControls.ListItem();
            Collection<int> MyCollection = new Collection<int>();
            //clear session var
            Session["Sensors"] = "";
            LiveMonitoring.GlobalRemoteVars MyRem = new LiveMonitoring.GlobalRemoteVars();
            Session["Sensors"] += ddlMeters.SelectedValue;
            //For Each Mynode As TreeNode In tvSensors.Nodes
            //    For Each subnode As TreeNode In Mynode.ChildNodes
            //        If subnode.Checked Then
            //            Session("Sensors") += subnode.Value.ToString + ","
            //        End If
            //    Next
            //Next
            //MyCollection = MyRem.GetServerObjects(IIf(IsNothing(Session("SelectedSite")), Nothing, Session("SelectedSite")))'GetServerObjects 'server1.GetAll()
            //For Each MyItem In chkSensors.Items
            //    If MyItem.Selected = True Then 'clear old check
            //        'add to cameras
            //        Dim MyObject1 As Object
            //        Dim MyCnt As Integer = 0
            //        For Each MyObject1 In MyCollection
            //            If TypeOf MyObject1 Is LiveMonitoring.IRemoteLib.SensorDetails Then
            //                Dim MySensor As LiveMonitoring.IRemoteLib.SensorDetails = CType(MyObject1, LiveMonitoring.IRemoteLib.SensorDetails)
            //                If MySensor.ID = MyItem.Value Then
            //                    Session("Sensors") += MySensor.ID.ToString + ","
            //                    Exit For
            //                Else
            //                    MyCnt += 1
            //                End If
            //            End If
            //        Next
            //    End If
            //Next

        }
        public void FillOldSessionSensors()
        {
            //Dim MyItem As New Web.UI.WebControls.ListItem()
            //Dim MyCollection As New Collection
            //'clear session var
            //Session("Sensors") = ""
            //Dim MyRem As New LiveMonitoring.GlobalRemoteVars
            //MyCollection = MyRem.GetServerObjects(IIf(IsNothing(Session("SelectedSite")), Nothing, Session("SelectedSite")))'GetServerObjects 'server1.GetAll()
            //For Each MyItem In chkSensors.Items
            //    If MyItem.Selected = True Then 'clear old check
            //        'add to cameras
            //        Dim MyObject1 As Object
            //        Dim MyCnt As Integer = 0
            //        For Each MyObject1 In MyCollection
            //            If TypeOf MyObject1 Is LiveMonitoring.IRemoteLib.SensorDetails Then
            //                Dim MySensor As LiveMonitoring.IRemoteLib.SensorDetails = CType(MyObject1, LiveMonitoring.IRemoteLib.SensorDetails)
            //                If MySensor.ID = MyItem.Value Then
            //                    Session("Sensors") += MySensor.ID.ToString + ","
            //                    Exit For
            //                Else
            //                    MyCnt += 1
            //                End If
            //            End If
            //        Next
            //    End If
            //Next

        }
       
        public void MeteringBillingReport()
        {
             Load += Page_Load;
        
        }

        //protected void cmbCurrentSite_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    Session["SlectedSite"] = ddlCurrentSite.SelectedValue;
        //}
        public class MySite
        {
            public int siteID { get; set; }
            public string siteName { get; set; }
        }

        protected void ddlTarrif_SelectedIndexChanged(object sender, EventArgs e)
        {
            //LiveMonitoring.GlobalRemoteVars MyRem = new LiveMonitoring.GlobalRemoteVars();
            //List<LiveMonitoring.IRemoteLib.MeteringTariff> MyNewList = MyRem.LiveMonServer.GetMeteringTarrifNames();
            //bool Firstone = true;
            //foreach (LiveMonitoring.IRemoteLib.MeteringTariff MyTarrif in MyNewList)
            //{
            //    System.Web.UI.WebControls.ListItem MyIttem = new System.Web.UI.WebControls.ListItem();
            //    MyIttem.Text = MyTarrif.TarriffName;
            //    MyIttem.Value = MyTarrif.ID.ToString();
            //    ddlTarrif.Items.Add(MyIttem);
            //    if (Firstone)
            //    {
            //        MyIttem.Selected = true;
            //        Firstone = false;
            //    }
            //}
        }

        //protected void btnGenerate2_Click(object sender, EventArgs e)
        //{

        //}

        //protected void btnGenerate1_Click1(object sender, EventArgs e)
        //{

        //}
        //Load_Tarrifs();
        //private void Load_Tarrifs(
    }
}