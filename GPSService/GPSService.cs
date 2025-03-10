using Chetch.ChetchXMPP;
using Chetch.ChetchXMPP.Exceptions;
using Chetch.Database;
using Chetch.GPS;
using Chetch.Messaging;
using Chetch.Alarms;
using Microsoft.Extensions.Hosting.Systemd;

namespace Chetch.GPSService;


public class GPSService : ChetchXMPPService<GPSService>, AlarmManager.IAlarmRaiser
{
    #region Constants
    public const String COMMAND_STATUS = "status";
    public const String COMMAND_POSITION = "position";
    //public const String COMMAND_SATELLITES = "satellites";

    public const String BBALARMS_SERVICE = "bbalarms.service@openfire.bb.lan";

    public const String GPS_ALARM_DISCONNECDTED = "gps";
    #endregion

    #region Classes and Enums
    
    #endregion

    #region Properties
    public AlarmManager AlarmManager { get; set; } = new AlarmManager();

    #endregion

    #region Fields
    GPSManager gpsManager = new GPSManager();
    #endregion

    #region Constructors
    public GPSService(ILogger<GPSService> Logger) : base(Logger)
    {
        ChetchDbContext.Config = Config;

        
    }
    #endregion
    
    #region Alarm Registtration
    public void RegisterAlarms()
    {
        AlarmManager.RegisterAlarm(this, GPS_ALARM_DISCONNECDTED);
    }
    #endregion

    #region Service Lifecycle
    protected override Task Execute(CancellationToken stoppingToken)
    {
        try
        {
            AlarmManager.AlarmDequeued += (mgr, alarm) => {
                try
                {
                    var msg = AlarmManager.CreateAlertMessage(alarm, BBALARMS_SERVICE);
                    SendMessage(msg);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, e.Message);
                }
            };
            AlarmManager.AddRaiser(this);
            AlarmManager.Run(() => ServiceConnected, stoppingToken);

            ServiceChanged += (sender, serviceEvent) => {
                if(serviceEvent == ServiceEvent.Connected)
                {
                    AlarmManager.Connect(this);
                }
            };
            
            gpsManager.ReceiverConnected += (sender, connected) => {
                if(!connected)
                {
                    AlarmManager.Raise(GPS_ALARM_DISCONNECDTED, AlarmManager.AlarmState.MODERATE, "GPS Receiver has disconnected");
                }
                else
                {
                    AlarmManager.Lower(GPS_ALARM_DISCONNECDTED, "GPS Receiver has connected");
                }
                Logger.LogWarning("Receiver connected: {0}", connected);
            };
            gpsManager.StartRecording();
            Logger.LogInformation("GPS manager started recording, receiver conneted: {0}", gpsManager.IsReceiverConnected);
        } 
        catch (Exception e)
        {
             Logger.LogError(e, e.Message);
             if(!gpsManager.IsReceiverConnected)
             {
                AlarmManager.Raise(GPS_ALARM_DISCONNECDTED, AlarmManager.AlarmState.MODERATE, "GPS Receiver cannot connect!");
             }
        }

        return base.Execute(stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        AlarmManager.Disconnect(this);
        gpsManager.StopRecording();


        return base.StopAsync(cancellationToken);
    }

    #endregion

    #region Command handling

    protected override void AddCommands()
    {
        AddCommand(COMMAND_POSITION, "Returns current position info (will error if GPS device is not receiving)");
        AddCommand(AlarmManager.COMMAND_LIST_ALARMS, "Lists alarms in this service");
        base.AddCommands();
    }

    protected override bool HandleCommandReceived(ServiceCommand command, List<object> arguments, Message response)
    {
        switch (command.Command)
        {
            case COMMAND_STATUS:

                return true;

            case COMMAND_POSITION:
                if(!gpsManager.IsReceiverConnected)
                {
                    throw new ChetchXMPPServiceException("No position data available as GPS receiver is currently not connected");
                }
                response.AddValue("Position", gpsManager.CurrentPosition);
                return true;

            /*case COMMAND_SATELLITES:
                return true;*/

            case AlarmManager.COMMAND_LIST_ALARMS:
                AlarmManager.AddAlarmsListToMessage(response);
                return true;

            default:
                return base.HandleCommandReceived(command, arguments, response);
        }
    }
    #endregion
}
