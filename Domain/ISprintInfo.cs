﻿using JiraApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public interface ISprintInfo
    {
        Task<SprintData> GetChordForSprintAsync(SprintAgile sprint);
    }
}
