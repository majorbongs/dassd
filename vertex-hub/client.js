const resourceName = GetCurrentResourceName();

emitNet('requestVertexHubResources/1b668804-40a6-478a-98c9-94bfbdff3070');

onNet('registerVertexHubResources/1b668804-40a6-478a-98c9-94bfbdff3070', (resourcesDto) => {
	const resources = JSON.parse(resourcesDto);
	for (const { fileName, cacheString } of resources) {
		RegisterStreamingFileFromCache(resourceName, fileName, cacheString);
	}
});