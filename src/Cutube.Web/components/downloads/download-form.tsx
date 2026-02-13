"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Sparkles, Video } from "lucide-react";
import { useForm, useWatch } from "react-hook-form";
import { z } from "zod";
import { useCreateDownload } from "@/hooks/use-downloads";
import { useVideoMetadata } from "@/hooks/use-video-metadata";
import { ErrorMessage } from "@/components/feedback/error-message";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";

const formSchema = z
  .object({
    url: z.string().url("Informe uma URL valida"),
    startTime: z.string().optional(),
    endTime: z.string().optional(),
    audioOnly: z.boolean(),
    customFilename: z.string().optional(),
  })
  .refine(
    (values) => {
      if (!values.startTime || !values.endTime) return true;
      return values.startTime < values.endTime;
    },
    {
      message: "Horario de inicio deve ser menor que horario final",
      path: ["endTime"],
    },
  );

type FormValues = z.infer<typeof formSchema>;

const defaultValues: FormValues = {
  url: "",
  startTime: "",
  endTime: "",
  audioOnly: false,
  customFilename: "",
};

export function DownloadForm() {
  const createMutation = useCreateDownload();

  const {
    control,
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues,
  });

  const urlValue = useWatch({ control, name: "url" }) ?? "";
  const audioOnlyValue = useWatch({ control, name: "audioOnly" }) ?? false;
  const metadataQuery = useVideoMetadata(urlValue);
  const metadata = !metadataQuery.isError ? metadataQuery.data : null;

  const onSubmit = async (values: FormValues) => {
    await createMutation.mutateAsync({
      ...values,
      outputPath: "/downloads",
      startTime: values.startTime || undefined,
      endTime: values.endTime || undefined,
      customFilename: values.customFilename || undefined,
    });

    reset(defaultValues);
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-xl">Novo download</CardTitle>
        <CardDescription>
          Cole a URL do video e acompanhe o progresso em tempo real via SignalR.
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-6">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="url">URL do video</Label>
            <Input
              id="url"
              type="url"
              placeholder="https://www.youtube.com/watch?v=..."
              {...register("url")}
            />
            {errors.url ? <p className="text-destructive text-sm">{errors.url.message}</p> : null}
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="customFilename">Nome personalizado</Label>
              <Input
                id="customFilename"
                placeholder="meu-video.mp4"
                {...register("customFilename")}
              />
            </div>

            <div className="bg-muted/50 flex items-center rounded-lg border px-3 py-2 text-sm">
              Os arquivos sao salvos no servidor em <code className="ml-1">/downloads</code>.
            </div>
          </div>

          <fieldset className="grid grid-cols-1 gap-4 rounded-lg border p-4 md:grid-cols-2">
            <legend className="px-1 text-sm font-medium">Intervalo de tempo (opcional)</legend>
            <div className="space-y-2">
              <Label htmlFor="startTime">Inicio (HH:MM:SS)</Label>
              <Input id="startTime" placeholder="00:00:30" {...register("startTime")} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="endTime">Fim (HH:MM:SS)</Label>
              <Input id="endTime" placeholder="00:02:00" {...register("endTime")} />
              {errors.endTime ? (
                <p className="text-destructive text-sm">{errors.endTime.message}</p>
              ) : null}
            </div>
          </fieldset>

          <div className="bg-muted/50 flex items-center justify-between rounded-lg border p-4">
            <div className="space-y-1">
              <Label htmlFor="audioOnly">Baixar apenas audio</Label>
              <p className="text-muted-foreground text-xs">
                Ideal para podcasts, entrevistas e musicas.
              </p>
            </div>
            <Switch
              id="audioOnly"
              checked={audioOnlyValue}
              onCheckedChange={(checked) => setValue("audioOnly", checked)}
            />
          </div>

          {createMutation.isError ? (
            <ErrorMessage
              message={
                createMutation.error instanceof Error
                  ? createMutation.error.message
                  : "Falha ao criar download"
              }
            />
          ) : null}

          <Button type="submit" className="w-full md:w-auto" disabled={createMutation.isPending}>
            {createMutation.isPending ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Criando download...
              </>
            ) : (
              <>
                <Sparkles className="h-4 w-4" />
                Iniciar download
              </>
            )}
          </Button>
        </form>

        {metadata ? (
          <div className="bg-accent/50 border-accent rounded-lg border p-4">
            <p className="text-muted-foreground mb-2 inline-flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
              <Video className="h-4 w-4" />
              Previa do video
            </p>
            <p className="text-base font-medium">{metadata.title}</p>
            <p className="text-muted-foreground text-sm">
              {metadata.uploader} · {metadata.duration}
            </p>
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}
