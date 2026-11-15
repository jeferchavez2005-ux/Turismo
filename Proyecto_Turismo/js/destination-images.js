export const destinationPlaceholder = "Img/waypath-placeholder.svg";

export function getDestinationImage(destination, catalog = []) {
    const direct = destination?.imagenUrl || destination?.imagen || destination?.imageUrl;
    if (isSafeImage(direct)) return direct;
    const id = String(destination?.destinoId || "");
    const name = normalize(destination?.destino || destination?.nombre);
    const match = catalog.find(item => (id && String(item.destinoId) === id)
        || (name && normalize(item.nombre) === name));
    return isSafeImage(match?.imagenUrl) ? match.imagenUrl : destinationPlaceholder;
}

function isSafeImage(value) {
    const image = String(value || "").trim();
    return Boolean(image) && !/^(?:javascript|data:text)\s*:/i.test(image);
}

function normalize(value) {
    return String(value || "").trim().toLocaleLowerCase("es");
}
