﻿namespace Fiero.Business
{
    public interface IAction
    {
        ActionName Name { get; }
        int? Cost { get; }
    }
}
