//
// BusinessTier:  business logic, acting as interface between UI and data store.
//

using System;
using System.Collections.Generic;
using System.Data;


namespace BusinessTier
{

  //
  // Business:
  //
  public class Business
  {
    //
    // Fields:
    //
    private string _DBFile;
    private DataAccessTier.Data dataTier;


    ///
    /// <summary>
    /// Constructs a new instance of the business tier.  The format
    /// of the filename should be either |DataDirectory|\filename.mdf,
    /// or a complete Windows pathname.
    /// </summary>
    /// <param name="DatabaseFilename">Name of database file</param>
    /// 
    public Business(string DatabaseFilename)
    {
      _DBFile = DatabaseFilename;

      dataTier = new DataAccessTier.Data(DatabaseFilename);
    }


    ///
    /// <summary>
    ///  Opens and closes a connection to the database, e.g. to
    ///  startup the server and make sure all is well.
    /// </summary>
    /// <returns>true if successful, false if not</returns>
    /// 
    public bool TestConnection()
    {
      return dataTier.OpenCloseConnection();
    }


    ///
    /// <summary>
    /// Returns all the CTA Stations, ordered by name.
    /// </summary>
    /// <returns>Read-only list of CTAStation objects</returns>
    /// 
    public IReadOnlyList<CTAStation> GetStations()
    {
      List<CTAStation> stations = new List<CTAStation>();

      try
      {

        //
        // TODO!
        //
        //DataAccessTier.Data dataTier = new DataAccessTier.Data(DatabaseFilename);
        string sql = @"
      Select StationID, Name FROM Stations 
      ORDER BY Name;
      ";

        DataSet result = dataTier.ExecuteNonScalarQuery(sql);

        foreach (DataRow row in result.Tables["TABLE"].Rows)
        {
          CTAStation s = new CTAStation(Convert.ToInt32(row["StationID"]), row["Name"].ToString());
          stations.Add(s);
        }

        return stations;

       
        //stations = result;

      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetStations: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      
    }


    ///
    /// <summary>
    /// Returns the CTA Stops associated with a given station,
    /// ordered by name.
    /// </summary>
    /// <returns>Read-only list of CTAStop objects</returns>
    ///
    public IReadOnlyList<CTAStop> GetStops(int stationID, string stationName)
    {
      List<CTAStop> stops = new List<CTAStop>();

      stationName = stationName.Replace("'", "''");

      try
      {

        //
        // TODO!
        //
        string sql = string.Format(@"
SELECT Stops.Name, Stops.StationID, StopID, Direction, ADA, Latitude, Longitude 
FROM Stops, Stations
WHERE Stops.StationID = Stations.StationID
AND Stations.Name = '{0}'
ORDER BY Stops.Name ASC;
", stationName);

        DataSet result = dataTier.ExecuteNonScalarQuery(sql);

        stationName = stationName.Replace("''", "'");

        foreach (DataRow row in result.Tables["TABLE"].Rows)
        {
          CTAStop s = new CTAStop(stationID, 
                                  row["Name"].ToString(), 
                                  stationID,
                                  row["Direction"].ToString(),
                                  Convert.ToBoolean(row["ADA"]),
                                  Convert.ToDouble(row["Latitude"]),
                                  Convert.ToDouble(row["Longitude"])
                                  );
          
          stops.Add(s);
        }

        return stops;


      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetStops: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return stops;
    }


    ///
    /// <summary>
    /// Returns the top N CTA Stations by ridership, 
    /// ordered by name.
    /// </summary>
    /// <returns>Read-only list of CTAStation objects</returns>
    /// 
    public IReadOnlyList<CTAStation> GetTopStations(int N)
    {
      if (N < 1)
        throw new ArgumentException("GetTopStations: N must be positive");

      List<CTAStation> stations = new List<CTAStation>();

      try
      {

        //
        // TODO!
        //
        string sql = string.Format(@"
SELECT Top {0} Name, Stations.StationID, Sum(DailyTotal) As TotalRiders 
FROM Riderships
INNER JOIN Stations ON Riderships.StationID = Stations.StationID 
GROUP BY Stations.StationID, Name
ORDER BY TotalRiders DESC;
", N);

        DataSet result = dataTier.ExecuteNonScalarQuery(sql);

        foreach (DataRow row in result.Tables["TABLE"].Rows)
        {
          CTAStation s = new CTAStation(Convert.ToInt32(row["StationID"]), row["Name"].ToString());
          stations.Add(s);
        }

      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetTopStations: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }

      return stations;
    }

    ///
    /// <summary>
    /// Returns all the CTA Stations, ordered by name.
    /// </summary>
    /// <returns>Read-only list of CTAStation objects</returns>
    /// 
    public DailyInfo GetStationInfo(string stationName)
    {
      //CTAStation station = new CTAStation()
      stationName = stationName.Replace("'", "''");

      try
      {

        //
        // TODO!
        //
        //DataAccessTier.Data dataTier = new DataAccessTier.Data(DatabaseFilename);
        string sql = string.Format(@"
SELECT Riderships.StationID, TypeOfDay, Sum(DailyTotal) AS Total
FROM Stations
INNER JOIN Riderships
ON Stations.StationID = Riderships.StationID
WHERE Stations.Name = '{0}'
GROUP BY Riderships.TypeOfDay, Riderships.StationID
ORDER BY Riderships.TypeOfDay;
", stationName);

        //MessageBox.Show(sql);

        DataSet result = dataTier.ExecuteNonScalarQuery(sql);

        //
        // we should get back 3 rows:
        //   row 0:  "A" for saturday
        //   row 1:  "U" for sunday/holiday
        //   row 2:  "W" for weekday
        //
        System.Diagnostics.Debug.Assert(result.Tables["TABLE"].Rows.Count == 3);

        DataRow R1 = result.Tables["TABLE"].Rows[0];
        DataRow R2 = result.Tables["TABLE"].Rows[1];
        DataRow R3 = result.Tables["TABLE"].Rows[2];

        //int stationID = Convert.ToInt32(R1["StationID"]);  // all rows have same station ID:
        //this.txtStationID.Text = stationID.ToString();

        System.Diagnostics.Debug.Assert(R1["TypeOfDay"].ToString() == "A");
        int total = Convert.ToInt32(R1["Total"]);
        //this.txtSaturdayRidership.Text = total.ToString("#,##0");

        System.Diagnostics.Debug.Assert(R2["TypeOfDay"].ToString() == "U");
        int total2 = Convert.ToInt32(R2["Total"]);
        //this.txtSundayHolidayRidership.Text = total.ToString("#,##0");

        System.Diagnostics.Debug.Assert(R3["TypeOfDay"].ToString() == "W");
        int total3 = Convert.ToInt32(R3["Total"]);
        //this.txtWeekdayRidership.Text = total.ToString("#,##0");

        DailyInfo day = new DailyInfo(total, total2, total3);

        return day;
        


        //stations = result;

      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetStationInfo: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }


    }

    public TotalInfo GetTotalInfo(string stationName)
    {
      //CTAStation station = new CTAStation()
      stationName = stationName.Replace("'", "''");

      try
      {

        //
        // TODO!
        //
        //DataAccessTier.Data dataTier = new DataAccessTier.Data(DatabaseFilename);
        string sql = string.Format(@"
SELECT Sum(Convert(bigint,DailyTotal)) As TotalOverall
FROM Riderships;
");

        //MessageBox.Show(sql);

        object r = dataTier.ExecuteScalarQuery(sql);
        long totalOverall = Convert.ToInt64(r);

        string sql2 = string.Format(@"
SELECT Sum(DailyTotal) As TotalRiders, 
       Avg(DailyTotal) As AvgRiders
FROM Riderships
INNER JOIN Stations ON Riderships.StationID = Stations.StationID
WHERE Name = '{0}';
", stationName);

        //MessageBox.Show(sql);

        DataSet result = dataTier.ExecuteNonScalarQuery(sql2);

        System.Diagnostics.Debug.Assert(result.Tables["TABLE"].Rows.Count == 1);
        DataRow R = result.Tables["TABLE"].Rows[0];

        int stationTotal = Convert.ToInt32(R["TotalRiders"]);
        double stationAvg = Convert.ToDouble(R["AvgRiders"]);
        double percentage = ((double)stationTotal) / totalOverall * 100.0;

        TotalInfo total = new TotalInfo(stationTotal, stationAvg, percentage);

        return total;

        //stations = result;

      }
      catch (Exception ex)
      {
        string msg = string.Format("Error in Business.GetStationInfo: '{0}'", ex.Message);
        throw new ApplicationException(msg);
      }


    }


  }//class
}//namespace
