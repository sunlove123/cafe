﻿using cafe.Shared;

namespace cafe.Client
{
    public interface IClientFactory
    {
        IChefServer RestClientForChefServer();
        IJobServer RestClientForJobServer();
        IProductServer<ProductStatus> RestClientForInspecServer();
    }
}