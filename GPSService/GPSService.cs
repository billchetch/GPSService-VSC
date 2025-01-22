using Chetch.ChetchXMPP;
using Chetch.ChetchXMPP.Exceptions;
using Chetch.Database;
using Chetch.GPS;
using Chetch.Messaging;

namespace Chetch.GPSService;


public class GPSService : ChetchXMPPService<GPSService>
{
    #region Constants
    public const String COMMAND_STATUS = "status";
    public const String COMMAND_POSITION = "position";
    //public const String COMMAND_SATELLITES = "satellites";
    #endregion

    #region Classes and Enums
    
    #endregion

    #region Fields
    GPSManager gpsManager = new GPSManager();

    #endregion

    public GPSService(ILogger<GPSService> Logger) : base(Logger)
    {
        ChetchDbContext.Config = Config;

        
    }

    #region Service Lifecycle
    protected override Task Execute(CancellationToken stoppingToken)
    {
        try
        {
            gpsManager.StartRecording();
            Logger.LogInformation("GPS manager started recording, receiver conneted: {0}", gpsManager.IsReceiverConnected);
        } 
        catch (Exception e)
        {
             Logger.LogError(e, e.Message);
        }

        return base.Execute(stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        gpsManager.StopRecording();


        return base.StopAsync(cancellationToken);
    }

    #endregion

    #region Command handling

    protected override void AddCommands()
    {
        AddCommand(COMMAND_POSITION, "Returns current position info (will error if GPS device is not receiving)");

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
                    throw new ChetchXMPPServiceException("No position data available as device is currently not connected");
                }
                response.AddValue("Position", gpsManager.CurrentPosition);
                return true;

            /*case COMMAND_SATELLITES:
                return true;*/

            default:
                return base.HandleCommandReceived(command, arguments, response);
        }
    }
    #endregion 
}
