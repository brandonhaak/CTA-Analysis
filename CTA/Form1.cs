// 
// N-tier C# and SQL program to analyze CTA ridership Data
// 
// BRANDON HAAK 
// U. of Illinois, Chicago 
// CS341, Fall 2017 
// Project 08
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace CTA
{

  public partial class Form1 : Form
  {
    private string BuildConnectionString()
    {
      string version = "MSSQLLocalDB";
      string filename = this.txtDatabaseFilename.Text;

      string connectionInfo = String.Format(@"Data Source=(LocalDB)\{0};AttachDbFilename={1};Integrated Security=True;", version, filename);

      return connectionInfo;
    }

    public Form1()
    {
      InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      //
      // setup GUI:
      //
      this.lstStations.Items.Add("");
      this.lstStations.Items.Add("[ Use File>>Load to display L stations... ]");
      this.lstStations.Items.Add("");

      this.lstStations.ClearSelected();

      toolStripStatusLabel1.Text = string.Format("Number of stations:  0");

      // 
      // open-close connect to get SQL Server started:
      //

      try
      {
        string filename = this.txtDatabaseFilename.Text;

        BusinessTier.Business bizTier;
        bizTier = new BusinessTier.Business(filename);

        bizTier.TestConnection();
      }
      catch
      {
        //
        // ignore any exception that occurs, goal is just to startup
        //
      }
    }


    //
    // File>>Exit:
    //
    private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    IReadOnlyList<BusinessTier.CTAStation> stations;
    IReadOnlyList<BusinessTier.CTAStop> stops;
    //
    // File>>Load Stations:
    //
    private void toolStripMenuItem2_Click(object sender, EventArgs e)
    {
      //
      // clear the UI of any current results:
      //
      ClearStationUI(true /*clear stations*/);

      //Load Stations
      BusinessTier.Business biztier;
      biztier = new BusinessTier.Business(
        this.txtDatabaseFilename.Text);

      stations = biztier.GetStations();

      foreach (var s in stations)
      {
        
        this.lstStations.Items.Add(s.Name);
      }

      toolStripStatusLabel1.Text = string.Format("Number of stations:  {0:#,##0}", stations.Count());
      
    }
    //
    // User has clicked on a station for more info:
    //
    private void lstStations_SelectedIndexChanged(object sender, EventArgs e)
    {
      // sometimes this event fires, but nothing is selected...
      if (this.lstStations.SelectedIndex < 0)   // so return now in this case:
        return;

      //
      // clear GUI in case this fails:
      //
      ClearStationUI();

      //
      // now display info about selected station:
      //

      string stationName = this.lstStations.Text;
      //stationName = stationName.Replace("'", "''");

      try
      {

        BusinessTier.Business biztier;
        biztier = new BusinessTier.Business(
          this.txtDatabaseFilename.Text);

        foreach (var s in stations)
        {
          if (s.Name == stationName)
          {

            //Get Ridership Info
            BusinessTier.DailyInfo day1 = biztier.GetStationInfo(s.Name);
            this.txtSaturdayRidership.Text = string.Format("{0:#,##0}", day1.A);
            this.txtSundayHolidayRidership.Text = string.Format("{0:#,##0}", day1.U);
            this.txtWeekdayRidership.Text = string.Format("{0:#,##0}", day1.W);
           
            this.txtStationID.Text = string.Format("{0}", s.ID);

            //Get total info
            BusinessTier.TotalInfo info = biztier.GetTotalInfo(s.Name);
            this.txtTotalRidership.Text = string.Format("{0:#,##0}", info.TRidership);
            this.txtAvgDailyRidership.Text = string.Format("{0:#,##0}/day", info.AvgRidership);
            this.txtPercentRidership.Text = string.Format("{0:0.00}%", info.PctRidership);


            //Get Stops
            stops = biztier.GetStops(s.ID, s.Name);
            foreach (var stop in stops)
            {
              //For stop info
              this.lstStops.Items.Add(stop.Name);
              this.txtDirection.Text = string.Format("{0}", stop.Direction);
              this.txtLocation.Text = string.Format("({0}, {1})", stop.Latitude, stop.Longitude);
              if (stop.ADA == true)
                this.txtAccessible.Text = "Yes";
              else
                this.txtAccessible.Text = "No";

            }
          }
              
        }

      }

      
      catch (Exception ex)
      {
        string msg = string.Format("Error: '{0}'.", ex.Message);
        MessageBox.Show(msg);
      }
      finally
      {
        
      }
    }
    
    private void ClearStationUI(bool clearStatations = false)
    {
      ClearStopUI();

      this.txtTotalRidership.Clear();
      this.txtTotalRidership.Refresh();

      this.txtAvgDailyRidership.Clear();
      this.txtAvgDailyRidership.Refresh();

      this.txtPercentRidership.Clear();
      this.txtPercentRidership.Refresh();

      this.txtStationID.Clear();
      this.txtStationID.Refresh();

      this.txtWeekdayRidership.Clear();
      this.txtWeekdayRidership.Refresh();
      this.txtSaturdayRidership.Clear();
      this.txtSaturdayRidership.Refresh();
      this.txtSundayHolidayRidership.Clear();
      this.txtSundayHolidayRidership.Refresh();

      this.lstStops.Items.Clear();
      this.lstStops.Refresh();

      if (clearStatations)
      {
        this.lstStations.Items.Clear();
        this.lstStations.Refresh();
      }
    }


    //
    // user has clicked on a stop for more info:
    //
    private void lstStops_SelectedIndexChanged(object sender, EventArgs e)
    {
      // sometimes this event fires, but nothing is selected...
      if (this.lstStops.SelectedIndex < 0)   // so return now in this case:
        return; 

      //
      // clear GUI in case this fails:
      //
      ClearStopUI();

      //
      // now display info about this stop:
      //
      string stopName = this.lstStops.Text;
      stopName = stopName.Replace("'", "''");

      SqlConnection db = null;

      try
      {
        db = new SqlConnection(BuildConnectionString());
        db.Open();

        SqlCommand cmd = new SqlCommand();
        cmd.Connection = db;

        //
        // Let's get some info about the stop:
        //
        // NOTE: we want to use station id, not stop name,
        // because stop name is not unique.  Example: the
        // stop "Damen (Loop-bound)".s
        //
        string sql = string.Format(@"
SELECT StopID, Direction, ADA, Latitude, Longitude
FROM Stops
WHERE Name = '{0}' AND
      StationID = {1};
", stopName, this.txtStationID.Text);

        //MessageBox.Show(sql);

        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
        DataSet ds = new DataSet();

        cmd.CommandText = sql;
        adapter.Fill(ds);

        System.Diagnostics.Debug.Assert(ds.Tables["TABLE"].Rows.Count == 1);
        DataRow R = ds.Tables["TABLE"].Rows[0];

        // handicap accessible?
        bool accessible = Convert.ToBoolean(R["ADA"]);

        if (accessible)
          this.txtAccessible.Text = "Yes";
        else
          this.txtAccessible.Text = "No";

        // direction of travel:
        this.txtDirection.Text = R["Direction"].ToString();

        // lat/long position:
        this.txtLocation.Text = string.Format("({0:00.0000}, {1:00.0000})", 
          Convert.ToDouble(R["Latitude"]), 
          Convert.ToDouble(R["Longitude"]));

        //
        // now we need to know what lines are associated 
        // with this stop:
        //
        int stopID = Convert.ToInt32(R["StopID"]);

        sql = string.Format(@"
SELECT Color
FROM Lines
INNER JOIN StopDetails ON Lines.LineID = StopDetails.LineID
INNER JOIN Stops ON StopDetails.StopID = Stops.StopID
WHERE Stops.StopID = {0}
ORDER BY Color ASC;
", stopID);

        //MessageBox.Show(sql);

        ds.Clear();

        cmd.CommandText = sql;
        adapter.Fill(ds);

        // display colors:
        foreach (DataRow row in ds.Tables["TABLE"].Rows)
        {
          this.lstLines.Items.Add(row["Color"].ToString());
        }
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error: '{0}'.", ex.Message);
        MessageBox.Show(msg);
      }
      finally
      {
        if (db != null && db.State == ConnectionState.Open)
          db.Close();
      }
    }

    private void ClearStopUI()
    {
      this.txtAccessible.Clear();
      this.txtAccessible.Refresh();

      this.txtDirection.Clear();
      this.txtDirection.Refresh();

      this.txtLocation.Clear();
      this.txtLocation.Refresh();

      this.lstLines.Items.Clear();
      this.lstLines.Refresh();
    }


    //
    // Top-10 stations in terms of ridership:
    //
    private void top10StationsByRidershipToolStripMenuItem_Click(object sender, EventArgs e)
    {
      //
      // clear the UI of any current results:
      //
      ClearStationUI(true /*clear stations*/);

      //
      // now load top-10 stations:
      //

      try
      {
        BusinessTier.Business biztier;
        biztier = new BusinessTier.Business(
          this.txtDatabaseFilename.Text);

        stations = biztier.GetTopStations(10);

        foreach (var s in stations)
        {
          this.lstStations.Items.Add(s.Name);
        }

        toolStripStatusLabel1.Text = string.Format("Number of stations:  {0:#,##0}", stations.Count());
      }
      catch (Exception ex)
      {
        string msg = string.Format("Error: '{0}'.", ex.Message);
        MessageBox.Show(msg);
      }
      finally
      {
        
      }
    }

    private void txtDatabaseFilename_TextChanged(object sender, EventArgs e)
    {

    }
  }//class
}//namespace
