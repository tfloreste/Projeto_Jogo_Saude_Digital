INCLUDE globals.ink

{ last_finished_cutscene:
- 1: -> para_casa_de_a
- 3: -> para_escola
- 4: -> retorno_casa_de_a
- else: -> developer_message
}

=== para_casa_de_a ===
{PLAYER_ACTOR}: Se me lembro bem a casa do {format_name(a_name)} fica no canto superior esquerdo da cidade.
{PLAYER_ACTOR}: É bem fácil de achar, já que a mãe dele {format_important_text("gosta MUITO de flores")}.
-> END

=== para_escola ===
{PLAYER_ACTOR}: Agora eu preciso voltar para a escola conversar com a professora {format_name(professor_name)}.
{PLAYER_ACTOR}: A escola fica no centro da cidade, no canto direito.
-> END

=== retorno_casa_de_a ===
{PLAYER_ACTOR}: Agora que eu tirei algumas das dúvidas que tinha sobre ansiedade, preciso voltar para a casa do {format_name(a_name)} falar com ele.
-> END

=== developer_message ===
Desenvolvedor: Oh! Parece que alguma coisa errada aconteceu.
Desenvolvedor: Você realmente não deveria poder clicar nesse botão neste momento. Sinto muito!
-> END
