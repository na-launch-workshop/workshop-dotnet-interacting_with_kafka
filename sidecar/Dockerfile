# Not built or used in this workshop. The price-api sidecar runs the shared
# workspace-images/electricity-price-api image, which is built from the Python version
# of this lab (sidecar/ in workshop-python-interacting_with_kafka) by the
# cluster/workspace-images chart in ocp-dev-days-rdshw-gitops.
#
# This Dockerfile packages the equivalent C# implementation the same way, as a reference.
# Both serve the same JSON on GET :5000/price, so either image works with the dashboard.

FROM registry.access.redhat.com/ubi8/dotnet-80:8.0 AS build
COPY --chown=1001:0 PriceApi/ .
RUN dotnet publish -c Release -o /opt/app-root/publish

FROM registry.access.redhat.com/ubi8/dotnet-80-runtime:8.0
COPY --from=build /opt/app-root/publish .
EXPOSE 5000
CMD ["dotnet", "PriceApi.dll"]
