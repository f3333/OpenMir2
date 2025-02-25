using System.Diagnostics;
using NLog;
using SystemModule.Generation.Entities;

namespace GameSrv.World.Threads
{
    public class GeneratorProcessor : TimerScheduledService
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly StandardRandomizer _standardRandomizer = new StandardRandomizer();
        private readonly Stopwatch sw = new Stopwatch();

        public GeneratorProcessor() : base(TimeSpan.FromSeconds(10), "GeneratorProcessor")
        {

        }

        public override void Initialize(CancellationToken cancellationToken)
        {
            GenerateIdThread();
        }

        protected override Task ExecuteInternal(CancellationToken stoppingToken)
        {
            GenerateIdThread();
            return Task.CompletedTask;
        }

        protected override void Startup(CancellationToken stoppingToken)
        {
            _logger.Info("Id生成器启动...");
        }

        protected override void Stopping(CancellationToken stoppingToken)
        {
            _logger.Info("Id生成器停止...");
        }

        private void GenerateIdThread()
        {
            if (M2Share.ActorMgr.GetGenerateQueueCount() < 20000)
            {
                sw.Reset();
                sw.Start();
                for (var i = 0; i < 100000; i++)
                {
                    var sequence = _standardRandomizer.NextInteger();
                    if (M2Share.ActorMgr.ContainsKey(sequence))
                    {
                        while (true)
                        {
                            sequence = _standardRandomizer.NextInteger();
                            if (!M2Share.ActorMgr.ContainsKey(sequence))
                            {
                                break;
                            }
                        }
                    }
                    while (sequence < 0)
                    {
                        sequence = Environment.TickCount + HUtil32.Sequence();
                        if (sequence > 0) break;
                    }
                    M2Share.ActorMgr.AddToQueue(sequence);
                }
                sw.Stop();
                _logger.Debug($"Id生成完毕 耗时:{sw.Elapsed} 可用数:[{M2Share.ActorMgr.GetGenerateQueueCount()}]");
            }
        }
    }
}