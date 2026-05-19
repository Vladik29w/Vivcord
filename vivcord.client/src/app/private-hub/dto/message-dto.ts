export interface messageDTO {
  id: string | number
  senderId: string
  text: string
  status: messageStatus
}
export type messageStatus = "sending" | "sent" | "error"
