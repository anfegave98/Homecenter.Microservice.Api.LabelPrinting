/*
 * Generador de datos semilla del submodulo de impresion de ETQ.
 *
 * Toma el ZPL real del anexo `responseEtq.json` y construye los mocks de ordenes,
 * etiquetas e inventario que cubren los casos de prueba CP-01 a CP-13 del plan.
 *
 * Uso: node mocks/_generate.js
 */
const fs = require('fs');
const path = require('path');

const zpl = require('./responseEtq.sample.json').response.zpl;

const zones = [
  { code: 'ZONA-PICKING-A', name: 'Zona Picking A' },
  { code: 'ZONA-PICKING-B', name: 'Zona Picking B' },
  { code: 'ZONA-DESPACHO', name: 'Zona Despacho' }
];

const products = [
  { productCode: 'PROD-001', productDescription: 'Martillo 16oz' },
  { productCode: 'PROD-002', productDescription: 'Guantes de seguridad' },
  { productCode: 'PROD-003', productDescription: 'Taladro percutor 1/2"' },
  { productCode: 'PROD-004', productDescription: 'Cemento gris 50kg' },
  { productCode: 'PROD-005', productDescription: 'Pintura vinilo blanco 1gl' },
  { productCode: 'PROD-006', productDescription: 'Tornillo autoperforante x100' }
];

/**
 * Cada orden esta construida para ejercitar un caso de prueba concreto.
 * El primer documento es el del anexo original, con su error de sintaxis corregido.
 */
const orders = [
  {
    requestId: 'REQ-20260605-001',
    requestDateTime: '2026-06-05T10:15:00-05:00',
    requestedBy: 'usuario.operacion',
    zone: 'ZONA-PICKING-A',
    document: { documentType: 'NOTA_PEDIDO', documentNumber: 'NP-458721', status: 'LIBERADA' },
    labels: [{ etqId: 'ETQ-10001', lpnId: 'LPN-000987654', isPreGenerated: true, templateCode: 'TPL-ETQ-STD-4X6', zpl }],
    products: [
      { productCode: 'PROD-001', productDescription: 'Martillo 16oz', requestedQty: 2, uom: 'UND' },
      { productCode: 'PROD-002', productDescription: 'Guantes de seguridad', requestedQty: 1, uom: 'PAR' }
    ],
    reprintReason: null,
    testCase: 'CP-07 impresion exitosa · CP-08/09/10 reimpresion (anexo original corregido)'
  },
  {
    requestId: 'REQ-20260605-002',
    requestDateTime: '2026-06-05T10:20:00-05:00',
    requestedBy: 'usuario.operacion',
    zone: 'ZONA-PICKING-A',
    document: { documentType: 'NOTA_PEDIDO', documentNumber: 'NP-458722', status: 'ANULADA' },
    labels: [{ etqId: 'ETQ-10002', lpnId: 'LPN-000987655', isPreGenerated: true, templateCode: 'TPL-ETQ-STD-4X6', zpl }],
    products: [{ productCode: 'PROD-001', productDescription: 'Martillo 16oz', requestedQty: 1, uom: 'UND' }],
    reprintReason: null,
    testCase: 'CP-02 documento ANULADA'
  },
  {
    requestId: 'REQ-20260605-003',
    requestDateTime: '2026-06-05T10:25:00-05:00',
    requestedBy: 'usuario.operacion',
    zone: 'ZONA-PICKING-B',
    document: { documentType: 'NOTA_PEDIDO', documentNumber: 'NP-458723', status: 'DEVUELTA' },
    labels: [{ etqId: 'ETQ-10003', lpnId: 'LPN-000987656', isPreGenerated: true, templateCode: 'TPL-ETQ-STD-4X6', zpl }],
    products: [{ productCode: 'PROD-003', productDescription: 'Taladro percutor 1/2"', requestedQty: 1, uom: 'UND' }],
    reprintReason: null,
    testCase: 'CP-03 documento DEVUELTA'
  },
  {
    requestId: 'REQ-20260605-004',
    requestDateTime: '2026-06-05T10:30:00-05:00',
    requestedBy: 'usuario.operacion',
    zone: 'ZONA-PICKING-B',
    document: { documentType: 'NOTA_PEDIDO', documentNumber: 'NP-458724', status: 'LIBERADA' },
    labels: [{ etqId: 'ETQ-10004', lpnId: 'LPN-000987657', isPreGenerated: true, templateCode: 'TPL-ETQ-STD-4X6', zpl }],
    products: [{ productCode: 'PROD-004', productDescription: 'Cemento gris 50kg', requestedQty: 5, uom: 'UND' }],
    reprintReason: null,
    testCase: 'CP-04 disponibilidad insuficiente (solicita 5, hay 1)'
  },
  {
    requestId: 'REQ-20260605-005',
    requestDateTime: '2026-06-05T10:35:00-05:00',
    requestedBy: 'usuario.operacion',
    zone: 'ZONA-DESPACHO',
    document: { documentType: 'NOTA_PEDIDO', documentNumber: 'NP-458725', status: 'LIBERADA' },
    labels: [{ etqId: 'ETQ-10005', lpnId: 'LPN-000987658', isPreGenerated: true, templateCode: 'TPL-ETQ-STD-4X6', zpl }],
    products: [{ productCode: 'PROD-005', productDescription: 'Pintura vinilo blanco 1gl', requestedQty: 2, uom: 'UND' }],
    reprintReason: null,
    testCase: 'CP-05 producto con stock pero NO abastecido en la zona'
  },
  {
    requestId: 'REQ-20260605-006',
    requestDateTime: '2026-06-05T10:40:00-05:00',
    requestedBy: 'usuario.operacion',
    zone: 'ZONA-PICKING-A',
    document: { documentType: 'NOTA_PEDIDO', documentNumber: 'NP-458726', status: 'CREADA' },
    labels: [{ etqId: 'ETQ-10006', lpnId: 'LPN-000987659', isPreGenerated: true, templateCode: 'TPL-ETQ-STD-4X6', zpl }],
    products: [{ productCode: 'PROD-006', productDescription: 'Tornillo autoperforante x100', requestedQty: 4, uom: 'CAJ' }],
    reprintReason: null,
    testCase: 'CP-06 limite exacto: solicita 4, hay 4. Estado CREADA no bloquea impresion'
  }
];

/**
 * Disponibilidad producto x zona. Es la fuente de la Regla 3 y no existe en los anexos:
 * se construye aqui (supuesto H4 del plan de trabajo).
 */
const inventoryAvailability = [
  { productCode: 'PROD-001', zoneCode: 'ZONA-PICKING-A', availableQty: 10, isStocked: true },
  { productCode: 'PROD-001', zoneCode: 'ZONA-PICKING-B', availableQty: 0, isStocked: false },
  { productCode: 'PROD-001', zoneCode: 'ZONA-DESPACHO', availableQty: 5, isStocked: true },
  { productCode: 'PROD-002', zoneCode: 'ZONA-PICKING-A', availableQty: 4, isStocked: true },
  { productCode: 'PROD-002', zoneCode: 'ZONA-PICKING-B', availableQty: 2, isStocked: true },
  { productCode: 'PROD-003', zoneCode: 'ZONA-PICKING-B', availableQty: 7, isStocked: true },
  { productCode: 'PROD-004', zoneCode: 'ZONA-PICKING-B', availableQty: 1, isStocked: true },
  { productCode: 'PROD-005', zoneCode: 'ZONA-DESPACHO', availableQty: 50, isStocked: false },
  { productCode: 'PROD-006', zoneCode: 'ZONA-PICKING-A', availableQty: 4, isStocked: true }
];

const labels = orders.flatMap((order) =>
  order.labels.map((label) => ({
    etqId: label.etqId,
    lpnId: label.lpnId,
    documentNumber: order.document.documentNumber,
    isPreGenerated: label.isPreGenerated,
    templateCode: label.templateCode,
    zpl: label.zpl
  }))
);

const write = (fileName, payload) => {
  const target = path.join(__dirname, fileName);
  fs.writeFileSync(target, `${JSON.stringify(payload, null, 2)}\n`, 'utf8');
  console.log(`generado: ${fileName}`);
};

write('zones.json', zones);
write('products.json', products);
write('orders.json', orders);
write('labels.json', labels);
write('inventoryAvailability.json', inventoryAvailability);
