using System;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Settings
{
    /// <summary>
    /// <see cref="IProgressReset"/> over the repository, which holds the actual reset rules. This only adds the
    /// <see cref="Changed"/> notification.
    /// </summary>
    public sealed class ProgressResetService : IProgressReset
    {
        private readonly ProgressRepository _repository;

        public ProgressResetService(ProgressRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public event Action Changed;

        public void ResetPersonalBests()
        {
            _repository.ResetPersonalBests();
            Changed?.Invoke();
        }

        public void ResetPersonalBest(SpeedStars.Core.Race.RaceEventId eventId)
        {
            _repository.ResetPersonalBest(eventId);
            Changed?.Invoke();
        }

        public void ClearCustomRacers()
        {
            _repository.ClearCustomRacers();
            Changed?.Invoke();
        }

        public void ResetAll()
        {
            _repository.ResetAll();
            Changed?.Invoke();
        }
    }
}
