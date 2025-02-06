namespace Hagalaz.Services.Common.Model
{
    public record PagingMetaDataModel(int Total, int Page, int Limit) : PagingModel(Page, Limit);
}