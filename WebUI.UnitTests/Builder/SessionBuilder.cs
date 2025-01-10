using System.Collections.Immutable;
using WebUI.Domain;
using WebUI.Types;
using WebUI.UnitTests.Fakes;

namespace WebUI.UnitTests.Builder;
internal class SessionBuilder : EntityBuilder<Session, SessionBuilder>
{
    public SessionBuilder WithChampionshipId(long championshipId) => WithChampionshipId(new(championshipId));
    public SessionBuilder WithChampionshipId(ChampionshipId championshipId) => With(t => t.ChampionshipId, championshipId);

    public SessionBuilder OfEvent(long eventId) => OfEvent(new(eventId));
    public SessionBuilder OfEvent(EventId eventId) => With(s => s.EventId, eventId);

    public SessionBuilder WithRandomSessionId() => WithSessionId(IdGeneratorHelper.GenerateId());
    public SessionBuilder WithSessionId(long sessionId) => WithSessionId(new(sessionId));
    public SessionBuilder WithSessionId(SessionId sessionId) => With(c => c.SessionId, sessionId);

    public SessionBuilder WithName(string name) => With(s => s.Name, name);

    public SessionBuilder ThatHasStarted(IEnumerable<IFeature> features, IEnumerable<SessionParticipant> participants)
    {
        With(s => s.State, State.Running);
        With(s => s.Features, new(features));
        With(s => s.Participants, participants.ToImmutableList());
        return this;
    }

    public SessionBuilder ThatHasProgressedToTheEnd()
    {
        With(s => s.ElapsedLaps, Get(s => s.LapCount));
        var participants = Get(s => s.Participants);
        List<LapResult> lapResults = [];
        for (var i = 0; i < Get(s => s.LapCount); i++)
        {
            var lastLapResult = lapResults.LastOrDefault();
            Dictionary<DriverId, ParticipantLapResult> participantLapResults = [];
            for (var j = 0; j < participants.Count; j++)
            {
                var participant = participants[j];
                participantLapResults[participant.DriverId] = new ParticipantLapResult((ushort)(j+1), (lastLapResult?.Results[participant.DriverId].TotalTime ?? TimeSpan.Zero) + TimeSpan.FromSeconds(j+1), TimeSpan.FromSeconds(j+1));
            }
            lapResults.Add(new LapResult(participantLapResults.ToImmutableDictionary()));
        }

        With(s => s.LapResults, lapResults.Select((lr, i) => new KeyValuePair<ushort, LapResult>((ushort)i, lr)).ToImmutableDictionary());
        return this;
    }

    public override SessionBuilder ThatIsValid() => WithChampionshipId(1).OfEvent(1).WithSessionId(1).WithName("Test Session")
        .With(s => s.PreviousSessionHasFinished, true)
        .With<ushort>(s => s.LapCount, 1);
}
