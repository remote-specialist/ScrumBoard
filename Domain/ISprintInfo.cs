using Domain.Models;
using JiraApi.Models;

namespace Domain
{
    public interface ISprintInfo
    {
        Task<SprintData> GetChordForSprintAsync(SprintAgile sprint);
    }
}
