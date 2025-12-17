// TODO: Better API errors
export class ApiError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'ApiError';
    }

    static fromResponse(resp: Response): ApiError {
        return new ApiError(`API Error: ${resp}`);
    }
}

export class ProblemDetails {
    constructor(
        public type: string,
        public title: string,
        public status: number,
        public detail: string,
        public instance: string
    ) {
    }

    static fromObject(obj: any): ProblemDetails {
        if (obj.type.endsWith('/errors/validation')) {
            return ValidationProblemDetails.fromObject(obj);
        }
        return new ProblemDetails(obj.type, obj.title, obj.status, obj.detail, obj.instance);
    }
}

export class ValidationMessage {
    constructor(
        public value: any,
        public code: string,
        public message: string,
    ) {}

    static fromObject(obj: any): ValidationMessage {
        return new ValidationMessage(
            obj.value,
            obj.code,
            obj.message
        );
    }
}

export class ValidationProblemDetails extends ProblemDetails {
    constructor(
        type: string,
        title: string,
        status: number,
        detail: string,
        instance: string,
        public validationMessages: { [member: string]: ValidationMessage[] } = {}
    ) {
        super(type, title, status, detail, instance);
    }

    static fromObject(obj: any): ValidationProblemDetails {
        const messages: { [member: string]: ValidationMessage[] } = {};
        if (obj.validationMessages && typeof obj.validationMessages === 'object') {
            for (const member in obj.validationMessages) {
                if (Array.isArray(obj.validationMessages[member])) {
                    messages[member] = obj.validationMessages[member].map(ValidationMessage.fromObject);
                }
            }
        }
        return new ValidationProblemDetails(obj.type, obj.title, obj.status, obj.detail, obj.instance, messages);
    }
}

export class Hypermedia<Type> {
    constructor(
        public version: string,
        public data: Type
    ) {

    }

    static fromObject<Type>(obj: any, map: (obj: any) => Type): Hypermedia<Type> {
        return new Hypermedia<Type>(obj._meta.version, map(obj));
    }
}

export class Championship {
    constructor(
        public championshipId: string,
        public name: string,
        public features: {},
        public pointsSystems: []
    ) {
    }

    static fromObject(obj: any): Championship {
        return new Championship(obj.championshipId, obj.name, obj.features, obj.pointsSystems);
    }
}

export class CreateChampionshipRequest {
    constructor(
        public name: string,
        public features: {} = {},
        public pointsSystems: [] = [],
    ) {
    }

    static serialize(request: CreateChampionshipRequest): string {
        return JSON.stringify(request);
    }
}

export class UpdateChampionshipRequest {
    constructor(
        public name: string,
        public features: {} = {},
        public pointsSystems: [] = [],
    ) {
    }

    static serialize(request: UpdateChampionshipRequest): string {
        return JSON.stringify(request);
    }
}

export type ApiResponse<T> = T | ProblemDetails;

export type HypermediaResponse<T> = ApiResponse<Hypermedia<T>>;

export async function createChampionship(request: CreateChampionshipRequest): Promise<HypermediaResponse<Championship>> {
    const resp = await http('/api/championships', 'POST', { body: CreateChampionshipRequest.serialize(request) });
    return processHyperMediaResponse<Championship>(resp, Championship.fromObject);
}

export async function updateChampionship(championshipId: string, version: string, request: UpdateChampionshipRequest): Promise<HypermediaResponse<Championship>> {
    const body = UpdateChampionshipRequest.serialize(request);
    return await http('/api/championships/' + championshipId, 'PUT', { body, version })
        .then(resp => processHyperMediaResponse<Championship>(resp, Championship.fromObject));
}

type method = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';

interface HttpRequestOptions {
    body?: object | string;
    version?: string;
}

async function http(url: URL | string, method: method, options: HttpRequestOptions) {
    const headers: any = {};
    let body;
    if (options.body) {
        headers['Content-Type'] = 'application/json';
        headers['Accept'] = 'application/vnd.lss.hyp+json, application/problem+json';
        body = typeof options.body === 'string' ? options.body : JSON.stringify(options.body);
    }

    if (options.version) {
        headers['If-Match'] = `"${options.version}"`;
    }

    return await fetch(url.toString(), { method, headers, body });
}

async function processHyperMediaResponse<T>(resp: Response, map: (obj: any) => T): Promise<HypermediaResponse<T>> {
    if (resp.ok) {
        if (resp.headers.get('Content-Type')?.startsWith('application/vnd.lss.hyp+json')) {
            return Hypermedia.fromObject<T>(await resp.json(), map);
        } else {
            throw new ApiError(`Unexpected content type: ${resp.headers.get('Content-Type')}`);
        }
    } else if (resp.headers.get('Content-Type')?.startsWith('application/problem+json')) {
        return ProblemDetails.fromObject(await resp.json());
    } else {
        throw ApiError.fromResponse(resp);
    }
}
